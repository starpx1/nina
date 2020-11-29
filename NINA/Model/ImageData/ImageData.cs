#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.Enum;
using NINA.Utility.FileFormat.FITS;
using NINA.Utility.FileFormat.XISF;
using NINA.Utility.ImageAnalysis;
using NINA.Utility.RawConverter;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Model.ImageData {

    public class ImageData : IImageData {

        public ImageData(ushort[] input, int width, int height, int bitDepth, bool isBayered, ImageMetaData metaData)
            : this(
                  imageArray: new ImageArray(flatArray: input),
                  width: width,
                  height: height,
                  bitDepth: bitDepth,
                  isBayered: isBayered,
                  metaData: metaData) {
        }

        public ImageData(IImageArray imageArray, int width, int height, int bitDepth, bool isBayered, ImageMetaData metaData) {
            this.Data = imageArray;
            this.MetaData = metaData;
            isBayered = metaData.Camera.SensorType != SensorType.Monochrome ? true : isBayered;
            this.Properties = new ImageProperties(width: width, height: height, bitDepth: bitDepth, isBayered: isBayered, gain: metaData.Camera.Gain);
            this.StarDetectionAnalysis = new StarDetectionAnalysis();
            this.Statistics = new Nito.AsyncEx.AsyncLazy<IImageStatistics>(async () => await Task.Run(() => ImageStatistics.Create(this)));
        }

        public IImageArray Data { get; private set; }

        public ImageProperties Properties { get; private set; }

        public ImageMetaData MetaData { get; private set; }

        public Nito.AsyncEx.AsyncLazy<IImageStatistics> Statistics { get; private set; }

        public IStarDetectionAnalysis StarDetectionAnalysis { get; private set; }

        public IRenderedImage RenderImage() {
            return RenderedImage.Create(source: this.RenderBitmapSource(), rawImageData: this);
        }

        public BitmapSource RenderBitmapSource() {
            return ImageUtility.CreateSourceFromArray(this.Data, this.Properties, PixelFormats.Gray16);
        }

        #region "Save"

        /// <summary>
        ///  Saves file to application temp path
        /// </summary>
        /// <param name="fileType"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<string> PrepareSave(FileSaveInfo fileSaveInfo, CancellationToken cancelToken = default) {
            var actualPath = string.Empty;
            try {
                using (MyStopWatch.Measure()) {
                    actualPath = await SaveToDiskAsync(fileSaveInfo, cancelToken, false);
                    Logger.Debug($"Saving temporary image at {actualPath}");
                }
            } catch (OperationCanceledException ex) {
                throw ex;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw ex;
            } finally {
            }
            return actualPath;
        }

        /// <summary>
        /// Renames and moves file to destination according to pattern
        /// </summary>
        /// <param name="file"></param>
        /// <param name="targetPath"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public string FinalizeSave(string file, string pattern) {
            try {
                if (pattern.Contains(ImagePatternKeys.SensorTemp) && double.IsNaN(this.MetaData.Camera.Temperature) && !string.IsNullOrEmpty(this.Data.RAWType)) {
                    string sensorTemp = getSensorTempFromExifTool(file);
                    pattern = pattern.Replace(ImagePatternKeys.SensorTemp, sensorTemp);
                }

                var imagePatterns = GetImagePatterns();
                var fileName = imagePatterns.GetImageFileString(pattern);
                var extension = Path.GetExtension(file);
                var targetPath = Path.GetDirectoryName(file);
                var newFileName = Utility.Utility.GetUniqueFilePath(Path.Combine(targetPath, $"{fileName}{extension}"));

                var fi = new FileInfo(newFileName);
                if (!fi.Directory.Exists) {
                    fi.Directory.Create();
                }

                File.Move(file, newFileName);

                Logger.Info($"Saving image at {newFileName}");

                return newFileName;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw ex;
            } finally {
            }
        }

        private string getSensorTempFromExifTool(string file) {
            string tempString = string.Empty;
            try {
                string EXIFTOOLLOCATION = Path.Combine(NINA.Utility.Utility.APPLICATIONDIRECTORY, "Utility", "ExifTool", "exiftool.exe");
                var sb = new StringBuilder();

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = EXIFTOOLLOCATION;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardInput = true;
                startInfo.CreateNoWindow = true;
                startInfo.Arguments = $"-CameraTemperature {file}";
                process.StartInfo = startInfo;
                process.EnableRaisingEvents = true;

                process.OutputDataReceived += (object sender, System.Diagnostics.DataReceivedEventArgs e) => {
                    sb.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (object sender, System.Diagnostics.DataReceivedEventArgs e) => {
                    sb.AppendLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                Logger.Trace(sb.ToString());

                // remove whitespace and format
                tempString = sb.ToString().Replace(" ", "");
                tempString = tempString.Substring(tempString.IndexOf(':') + 1).ToLower().Trim();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            return tempString;
        }

        public ImagePatterns GetImagePatterns() {
            var p = new ImagePatterns();
            var metadata = this.MetaData;
            p.Set(ImagePatternKeys.Filter, metadata.FilterWheel.Filter);
            p.Set(ImagePatternKeys.ExposureTime, metadata.Image.ExposureTime);
            p.Set(ImagePatternKeys.ApplicationStartDate, Utility.Utility.ApplicationStartDate.ToString("yyyy-MM-dd"));
            p.Set(ImagePatternKeys.Date, metadata.Image.ExposureStart.ToString("yyyy-MM-dd"));

            // ExposureStart is initialized to DateTime.MinValue, and we cannot subtract time from that. Only evaluate
            // the $$DATEMINUS12$$ pattern if the time is at least 12 hours on from DateTime.MinValue.
            if (metadata.Image.ExposureStart > DateTime.MinValue.AddHours(12)) {
                p.Set(ImagePatternKeys.DateMinus12, metadata.Image.ExposureStart.AddHours(-12).ToString("yyyy-MM-dd"));
            }

            p.Set(ImagePatternKeys.Time, metadata.Image.ExposureStart.ToString("HH-mm-ss"));
            p.Set(ImagePatternKeys.DateTime, metadata.Image.ExposureStart.ToString("yyyy-MM-dd_HH-mm-ss"));
            p.Set(ImagePatternKeys.FrameNr, metadata.Image.ExposureNumber.ToString("0000"));
            p.Set(ImagePatternKeys.ImageType, metadata.Image.ImageType);
            p.Set(ImagePatternKeys.TargetName, metadata.Target.Name);

            if (metadata.Image.RecordedRMS != null) {
                p.Set(ImagePatternKeys.RMS, metadata.Image.RecordedRMS.Total);
                p.Set(ImagePatternKeys.RMSArcSec, metadata.Image.RecordedRMS.Total * metadata.Image.RecordedRMS.Scale);
            }

            if (metadata.Focuser.Position.HasValue) {
                p.Set(ImagePatternKeys.FocuserPosition, metadata.Focuser.Position.Value);
            }

            if (!double.IsNaN(metadata.Focuser.Temperature)) {
                p.Set(ImagePatternKeys.FocuserTemp, metadata.Focuser.Temperature);
            }

            if (metadata.Camera.Binning == string.Empty) {
                p.Set(ImagePatternKeys.Binning, "1x1");
            } else {
                p.Set(ImagePatternKeys.Binning, metadata.Camera.Binning);
            }

            if (!double.IsNaN(metadata.Camera.Temperature)) {
                p.Set(ImagePatternKeys.SensorTemp, metadata.Camera.Temperature);
            }

            if (metadata.Camera.Gain >= 0) {
                p.Set(ImagePatternKeys.Gain, metadata.Camera.Gain);
            }

            if (metadata.Camera.Offset >= 0) {
                p.Set(ImagePatternKeys.Offset, metadata.Camera.Offset);
            }

            if (metadata.Camera.USBLimit >= 0) {
                p.Set(ImagePatternKeys.USBLimit, metadata.Camera.USBLimit);
            }

            if (!double.IsNaN(this.StarDetectionAnalysis.HFR)) {
                p.Set(ImagePatternKeys.HFR, this.StarDetectionAnalysis.HFR);
            }

            if (!double.IsNaN(metadata.WeatherData.SkyQuality)) {
                p.Set(ImagePatternKeys.SQM, metadata.WeatherData.SkyQuality);
            }

            if (!string.IsNullOrEmpty(metadata.Camera.ReadoutModeName)) {
                p.Set(ImagePatternKeys.ReadoutMode, metadata.Camera.ReadoutModeName);
            }

            if (!string.IsNullOrEmpty(metadata.Camera.Name)) {
                p.Set(ImagePatternKeys.Camera, metadata.Camera.Name);
            }

            if (!string.IsNullOrEmpty(metadata.Telescope.Name)) {
                p.Set(ImagePatternKeys.Telescope, metadata.Telescope.Name);
            }

            if (!double.IsNaN(metadata.Rotator.Position)) {
                p.Set(ImagePatternKeys.RotatorAngle, metadata.Rotator.Position);
            }

            if (this.StarDetectionAnalysis.DetectedStars >= 0) {
                p.Set(ImagePatternKeys.StarCount, this.StarDetectionAnalysis.DetectedStars);
            }

            return p;
        }

        public async Task<string> SaveToDisk(FileSaveInfo fileSaveInfo, CancellationToken token, bool forceFileType = false) {
            string actualPath = string.Empty;
            try {
                using (MyStopWatch.Measure()) {
                    string tempPath = await SaveToDiskAsync(fileSaveInfo, token, forceFileType);
                    actualPath = FinalizeSave(tempPath, fileSaveInfo.FilePattern);
                }
            } catch (OperationCanceledException ex) {
                throw ex;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw ex;
            } finally {
            }
            return actualPath;
        }

        private Task<string> SaveToDiskAsync(FileSaveInfo fileSaveInfo, CancellationToken cancelToken, bool forceFileType = false) {
            return Task.Run(() => {
                string path = string.Empty;
                fileSaveInfo.FilePath = Path.Combine(fileSaveInfo.FilePath, Guid.NewGuid().ToString());

                if (!forceFileType && Data.RAWData != null) {
                    fileSaveInfo.FileType = FileTypeEnum.RAW;
                    path = SaveRAW(fileSaveInfo.FilePath);
                } else {
                    switch (fileSaveInfo.FileType) {
                        case FileTypeEnum.FITS:
                            path = SaveFits(fileSaveInfo);
                            break;

                        case FileTypeEnum.XISF:
                            path = SaveXisf(fileSaveInfo);
                            break;

                        case FileTypeEnum.TIFF:
                        default:
                            path = SaveTiff(fileSaveInfo);
                            break;
                    }
                }

                return path;
            }, cancelToken);
        }

        private string SaveRAW(string path) {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            IImageArray data = Data;
            string uniquePath = Utility.Utility.GetUniqueFilePath(path + "." + data.RAWType);
            File.WriteAllBytes(uniquePath, data.RAWData);
            return uniquePath;
        }

        private string SaveTiff(FileSaveInfo fileSaveInfo) {
            Directory.CreateDirectory(Path.GetDirectoryName(fileSaveInfo.FilePath));
            string uniquePath = Utility.Utility.GetUniqueFilePath(fileSaveInfo.FilePath + fileSaveInfo.GetExtension(".tif"));

            using (FileStream fs = new FileStream(uniquePath, FileMode.Create)) {
                TiffBitmapEncoder encoder = new TiffBitmapEncoder();

                switch (fileSaveInfo.TIFFCompressionType) {
                    case TIFFCompressionTypeEnum.LZW:
                        encoder.Compression = TiffCompressOption.Lzw;
                        break;

                    case TIFFCompressionTypeEnum.ZIP:
                        encoder.Compression = TiffCompressOption.Zip;
                        break;
                }

                encoder.Frames.Add(BitmapFrame.Create(RenderBitmapSource()));
                encoder.Save(fs);
            }

            return uniquePath;
        }

        private string SaveFits(FileSaveInfo fileSaveInfo) {
            FITS f = new FITS(
                Data.FlatArray,
                Properties.Width,
                Properties.Height
            );

            f.PopulateHeaderCards(MetaData);

            Directory.CreateDirectory(Path.GetDirectoryName(fileSaveInfo.FilePath));
            string uniquePath = Utility.Utility.GetUniqueFilePath(fileSaveInfo.FilePath + fileSaveInfo.GetExtension(".fits"));

            using (FileStream fs = new FileStream(uniquePath, FileMode.Create)) {
                f.Write(fs);
            }

            return uniquePath;
        }

        private string SaveXisf(FileSaveInfo fileSaveInfo) {
            XISFHeader header = new XISFHeader();

            header.AddImageMetaData(Properties, MetaData.Image.ImageType);

            header.Populate(MetaData);

            XISF img = new XISF(header);

            img.AddAttachedImage(Data.FlatArray, fileSaveInfo);

            Directory.CreateDirectory(Path.GetDirectoryName(fileSaveInfo.FilePath));
            string uniquePath = Utility.Utility.GetUniqueFilePath(fileSaveInfo.FilePath + fileSaveInfo.GetExtension(".xisf"));

            using (FileStream fs = new FileStream(uniquePath, FileMode.Create)) {
                img.Save(fs);
            }

            return uniquePath;
        }

        #endregion "Save"

        #region "Load"

        /// <summary>
        /// Loads an image from a given file path
        /// </summary>
        /// <param name="path">File Path to image</param>
        /// <param name="bitDepth">bit depth of each pixel</param>
        /// <param name="isBayered">Flag to indicate if the image is bayer matrix encoded</param>
        /// <param name="rawConverter">Which type of raw converter to use, when image is in RAW format</param>
        /// <param name="ct">Token to cancel operation</param>
        /// <returns></returns>
        public static Task<IImageData> FromFile(string path, int bitDepth, bool isBayered, RawConverterEnum rawConverter, CancellationToken ct = default) {
            return Task.Run(async () => {
                if (!File.Exists(path)) {
                    throw new FileNotFoundException();
                }
                BitmapDecoder decoder;
                switch (Path.GetExtension(path).ToLower()) {
                    case ".gif":
                        decoder = new GifBitmapDecoder(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                        return BitmapToImageArray(decoder, isBayered);

                    case ".tif":
                    case ".tiff":
                        decoder = new TiffBitmapDecoder(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                        return BitmapToImageArray(decoder, isBayered);

                    case ".jpg":
                    case ".jpeg":
                        decoder = new JpegBitmapDecoder(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                        return BitmapToImageArray(decoder, isBayered);

                    case ".png":
                        decoder = new PngBitmapDecoder(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                        return BitmapToImageArray(decoder, isBayered);

                    case ".xisf":
                        return await XISF.Load(new Uri(path), isBayered, ct);

                    case ".fit":
                    case ".fits":
                        return await FITS.Load(new Uri(path), isBayered, ct);

                    case ".cr2":
                    case ".cr3":
                    case ".nef":
                    case ".raf":
                    case ".raw":
                    case ".pef":
                    case ".dng":
                    case ".arw":
                    case ".orf":
                        return await RawToImageArray(path, bitDepth, rawConverter, ct);

                    default:
                        throw new NotSupportedException();
                }
            }, ct);
        }

        private static async Task<IImageData> RawToImageArray(string path, int bitDepth, RawConverterEnum rawConverter, CancellationToken ct) {
            using (var fs = new FileStream(path, FileMode.Open)) {
                using (var ms = new System.IO.MemoryStream()) {
                    await fs.CopyToAsync(ms);
                    var converter = RawConverter.CreateInstance(rawConverter);
                    var rawType = Path.GetExtension(path).ToLower().Substring(1);
                    var data = await converter.Convert(s: ms, bitDepth: bitDepth, rawType: rawType, metaData: new ImageMetaData(), token: ct);
                    return data;
                }
            }
        }

        private static IImageData BitmapToImageArray(BitmapDecoder decoder, bool isBayered) {
            var bmp = new FormatConvertedBitmap();
            bmp.BeginInit();
            bmp.Source = decoder.Frames[0];
            bmp.DestinationFormat = System.Windows.Media.PixelFormats.Gray16;
            bmp.EndInit();

            var stride = (bmp.PixelWidth * bmp.Format.BitsPerPixel + 7) / 8;
            var pixels = new ushort[bmp.PixelWidth * bmp.PixelHeight];
            bmp.CopyPixels(pixels, stride, 0);
            return new ImageData(pixels, bmp.PixelWidth, bmp.PixelHeight, 16, isBayered, new ImageMetaData());
        }

        #endregion "Load"
    }
}