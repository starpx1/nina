﻿#region "copyright"

/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Runtime.InteropServices;

namespace NINA.Equipment.Interfaces {

    public delegate void ToupTekAlikeCallback(ToupTekAlikeEvent tEvent);

    public interface IToupTekAlikeCameraSDK {
        string Category { get; }
        uint MaxSpeed { get; }
        bool MonoMode { get; }

        void get_Temperature(out short temp);

        void get_Option(ToupTekAlikeOption option, out int target);

        bool put_Option(ToupTekAlikeOption option, int v);

        void get_ExpTimeRange(out uint min, out uint max, out uint def);

        void get_Speed(out ushort speed);

        bool put_Speed(ushort value);

        bool get_ExpoAGain(out ushort gain);

        bool put_AutoExpoEnable(bool v);

        void get_Size(out int width, out int height);

        bool put_ROI(uint x, uint y, uint width, uint height);

        void get_ExpoAGainRange(out ushort min, out ushort max, out ushort def);

        bool put_ExpoAGain(ushort value);

        bool StartPullModeWithCallback(ToupTekAlikeCallback toupTekAlikeCallback);

        bool get_RawFormat(out uint fourCC, out uint bitDepth);

        bool PullImageV2(ushort[] data, int bitDepth, out ToupTekAlikeFrameInfo info);

        void Close();

        bool put_ExpoTime(uint µsTime);

        bool Trigger(ushort v);

        IToupTekAlikeCameraSDK Open(string id);

        string Version();
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ToupTekAlikeFrameInfo {
        public uint width;
        public uint height;
        public uint flag;         /* FRAMEINFO_FLAG_xxxx */
        public uint seq;          /* sequence number */
        public ulong timestamp;    /* microsecond */
    };

    public struct ToupTekAlikeResolution {
        public uint width;
        public uint height;
    };

    public struct ToupTekAlikeModel {
        public string name;         /* model name */
        public ulong flag;          /* sdk_FLAG_xxx, 64 bits */
        public uint maxspeed;       /* number of speed level, same as get_MaxSpeed(), the speed range = [0, maxspeed], closed interval */
        public uint preview;        /* number of preview resolution, same as get_ResolutionNumber() */
        public uint still;          /* number of still resolution, same as get_StillResolutionNumber() */
        public uint maxfanspeed;    /* maximum fan speed */
        public uint ioctrol;        /* number of input/output control */
        public float xpixsz;        /* physical pixel size */
        public float ypixsz;        /* physical pixel size */
        public ToupTekAlikeResolution[] res;
    };

    public struct ToupTekAlikeDeviceInfo {
        public string displayname; /* display name */
        public string id;          /* unique and opaque id of a connected camera */
        public ToupTekAlikeModel model;
    };

    [Flags]
    public enum ToupTekAlikeFlag : ulong {
        FLAG_CMOS = 0x00000001,   /* cmos sensor */
        FLAG_CCD_PROGRESSIVE = 0x00000002,   /* progressive ccd sensor */
        FLAG_CCD_INTERLACED = 0x00000004,   /* interlaced ccd sensor */
        FLAG_ROI_HARDWARE = 0x00000008,   /* support hardware ROI */
        FLAG_MONO = 0x00000010,   /* monochromatic */
        FLAG_BINSKIP_SUPPORTED = 0x00000020,   /* support bin/skip mode */
        FLAG_USB30 = 0x00000040,   /* usb3.0 */
        FLAG_TEC = 0x00000080,   /* Thermoelectric Cooler */
        FLAG_USB30_OVER_USB20 = 0x00000100,   /* usb3.0 camera connected to usb2.0 port */
        FLAG_ST4 = 0x00000200,   /* ST4 */
        FLAG_GETTEMPERATURE = 0x00000400,   /* support to get the temperature of the sensor */
        FLAG_HIGH_FULLWELL = 0x00000800,   /* high fullwell capacity */
        FLAG_RAW10 = 0x00001000,   /* pixel format, RAW 10bits */
        FLAG_RAW12 = 0x00002000,   /* pixel format, RAW 12bits */
        FLAG_RAW14 = 0x00004000,   /* pixel format, RAW 14bits */
        FLAG_RAW16 = 0x00008000,   /* pixel format, RAW 16bits */
        FLAG_FAN = 0x00010000,   /* cooling fan */
        FLAG_TEC_ONOFF = 0x00020000,   /* Thermoelectric Cooler can be turn on or off, support to set the target temperature of TEC */
        FLAG_ISP = 0x00040000,   /* ISP (Image Signal Processing) chip */
        FLAG_TRIGGER_SOFTWARE = 0x00080000,   /* support software trigger */
        FLAG_TRIGGER_EXTERNAL = 0x00100000,   /* support external trigger */
        FLAG_TRIGGER_SINGLE = 0x00200000,   /* only support trigger single: one trigger, one image */
        FLAG_BLACKLEVEL = 0x00400000,   /* support set and get the black level */
        FLAG_AUTO_FOCUS = 0x00800000,   /* support auto focus */
        FLAG_BUFFER = 0x01000000,   /* frame buffer */
        FLAG_DDR = 0x02000000,   /* use very large capacity DDR (Double Data Rate SDRAM) for frame buffer. The capacity is not less than one full frame */
        FLAG_CG = 0x04000000,   /* support Conversion Gain mode: HCG, LCG */
        FLAG_YUV411 = 0x08000000,   /* pixel format, yuv411 */
        FLAG_VUYY = 0x10000000,   /* pixel format, yuv422, VUYY */
        FLAG_YUV444 = 0x20000000,   /* pixel format, yuv444 */
        FLAG_RGB888 = 0x40000000,   /* pixel format, RGB888 */
        [Obsolete("Use FLAG_RAW10")]
        FLAG_BITDEPTH10 = FLAG_RAW10,   /* obsolete, same as FLAG_RAW10 */
        [Obsolete("Use FLAG_RAW12")]
        FLAG_BITDEPTH12 = FLAG_RAW12,   /* obsolete, same as FLAG_RAW12 */
        [Obsolete("Use FLAG_RAW14")]
        FLAG_BITDEPTH14 = FLAG_RAW14,   /* obsolete, same as FLAG_RAW14 */
        [Obsolete("Use FLAG_RAW16")]
        FLAG_BITDEPTH16 = FLAG_RAW16,   /* obsolete, same as FLAG_RAW16 */
        FLAG_RAW8 = 0x80000000,   /* pixel format, RAW 8 bits */
        FLAG_GMCY8 = 0x0000000100000000,  /* pixel format, GMCY, 8 bits */
        FLAG_GMCY12 = 0x0000000200000000,  /* pixel format, GMCY, 12 bits */
        FLAG_UYVY = 0x0000000400000000,  /* pixel format, yuv422, UYVY */
        FLAG_CGHDR = 0x0000000800000000,  /* Conversion Gain: HCG, LCG, HDR */
        FLAG_GLOBALSHUTTER = 0x0000001000000000,  /* global shutter */
        FLAG_FOCUSMOTOR = 0x0000002000000000,  /* support focus motor */
        FLAG_PRECISE_FRAMERATE = 0x0000004000000000,  /* support precise framerate & bandwidth, see OPTION_PRECISE_FRAMERATE & OPTION_BANDWIDTH */
        FLAG_HEAT = 0x0000008000000000,  /* support heat to prevent fogging up */
        FLAG_LOW_NOISE = 0x0000010000000000,  /* support low noise mode (Higher signal noise ratio, lower frame rate) */
        FLAG_LEVELRANGE_HARDWARE = 0x0000020000000000,  /* hardware level range, put(get)_LevelRangeV2 */
        FLAG_EVENT_HARDWARE = 0x0000040000000000,  /* hardware event, such as exposure start & stop */
        FLAG_LIGHTSOURCE = 0x0000080000000000,  /* light source */
        FLAG_FILTERWHEEL = 0x0000100000000000,  /* filter wheel */
        FLAG_GIGE = 0x0000200000000000,  /* GigE */
        FLAG_10GIGE = 0x0000400000000000   /* 10 Gige */
    };

    public enum ToupTekAlikeEvent : uint {
        EVENT_EXPOSURE = 0x0001, /* exposure time or gain changed */
        EVENT_TEMPTINT = 0x0002, /* white balance changed, Temp/Tint mode */
        EVENT_CHROME = 0x0003, /* reversed, do not use it */
        EVENT_IMAGE = 0x0004, /* live image arrived, use Toupcam_PullImage to get this image */
        EVENT_STILLIMAGE = 0x0005, /* snap (still) frame arrived, use Toupcam_PullStillImage to get this frame */
        EVENT_WBGAIN = 0x0006, /* white balance changed, RGB Gain mode */
        EVENT_TRIGGERFAIL = 0x0007, /* trigger failed */
        EVENT_BLACK = 0x0008, /* black balance changed */
        EVENT_FFC = 0x0009, /* flat field correction status changed */
        EVENT_DFC = 0x000a, /* dark field correction status changed */
        EVENT_ROI = 0x000b, /* roi changed */
        EVENT_LEVELRANGE = 0x000c, /* level range changed */
        EVENT_AUTOEXPO_CONV = 0x000d, /* auto exposure convergence */
        EVENT_AUTOEXPO_CONVFAIL = 0x000e, /* auto exposure once mode convergence failed */
        EVENT_ERROR = 0x0080, /* generic error */
        EVENT_DISCONNECTED = 0x0081, /* camera disconnected */
        EVENT_NOFRAMETIMEOUT = 0x0082, /* no frame timeout error */
        EVENT_AFFEEDBACK = 0x0083, /* auto focus feedback information */
        EVENT_FOCUSPOS = 0x0084, /* focus positon */
        EVENT_NOPACKETTIMEOUT = 0x0085, /* no packet timeout */
        EVENT_EXPO_START = 0x4000, /* hardware event: exposure start */
        EVENT_EXPO_STOP = 0x4001, /* hardware event: exposure stop */
        EVENT_TRIGGER_ALLOW = 0x4002, /* hardware event: next trigger allow */
        EVENT_HEARTBEAT = 0x4003, /* hardware event: heartbeat, can be used to monitor whether the camera is alive */
        EVENT_TRIGGER_IN = 0x4004, /* hardware event: trigger in */
        EVENT_FACTORY = 0x8001  /* restore factory settings */
    };

    public enum ToupTekAlikeOption : uint {
        OPTION_NOFRAME_TIMEOUT = 0x01,       /* no frame timeout: 0 => disable, positive value (>= 500) => timeout milliseconds. default: disable */
        OPTION_THREAD_PRIORITY = 0x02,       /* set the priority of the internal thread which grab data from the usb device.
                                                         Win: iValue: 0 = THREAD_PRIORITY_NORMAL; 1 = THREAD_PRIORITY_ABOVE_NORMAL; 2 = THREAD_PRIORITY_HIGHEST; 3 = THREAD_PRIORITY_TIME_CRITICAL; default: 1; see: https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setthreadpriority
                                                         Linux & macOS: The high 16 bits for the scheduling policy, and the low 16 bits for the priority; see: https://linux.die.net/man/3/pthread_setschedparam
                                                    */
        OPTION_RAW = 0x04,       /* raw data mode, read the sensor "raw" data. This can be set only BEFORE Toupcam_StartXXX(). 0 = rgb, 1 = raw, default value: 0 */
        OPTION_HISTOGRAM = 0x05,       /* 0 = only one, 1 = continue mode */
        OPTION_BITDEPTH = 0x06,       /* 0 = 8 bits mode, 1 = 16 bits mode */
        OPTION_FAN = 0x07,       /* 0 = turn off the cooling fan, [1, max] = fan speed */
        OPTION_TEC = 0x08,       /* 0 = turn off the thermoelectric cooler, 1 = turn on the thermoelectric cooler */
        OPTION_LINEAR = 0x09,       /* 0 = turn off the builtin linear tone mapping, 1 = turn on the builtin linear tone mapping, default value: 1 */
        OPTION_CURVE = 0x0a,       /* 0 = turn off the builtin curve tone mapping, 1 = turn on the builtin polynomial curve tone mapping, 2 = logarithmic curve tone mapping, default value: 2 */
        OPTION_TRIGGER = 0x0b,       /* 0 = video mode, 1 = software or simulated trigger mode, 2 = external trigger mode, 3 = external + software trigger, default value = 0 */
        OPTION_RGB = 0x0c,       /* 0 => RGB24; 1 => enable RGB48 format when bitdepth > 8; 2 => RGB32; 3 => 8 Bits Grey (only for mono camera); 4 => 16 Bits Grey (only for mono camera when bitdepth > 8); 5 => RGB64 */
        OPTION_COLORMATIX = 0x0d,       /* enable or disable the builtin color matrix, default value: 1 */
        OPTION_WBGAIN = 0x0e,       /* enable or disable the builtin white balance gain, default value: 1 */
        OPTION_TECTARGET = 0x0f,       /* get or set the target temperature of the thermoelectric cooler, in 0.1 degree Celsius. For example, 125 means 12.5 degree Celsius, -35 means -3.5 degree Celsius */
        OPTION_AUTOEXP_POLICY = 0x10,       /* auto exposure policy:
                                                            0: Exposure Only
                                                            1: Exposure Preferred
                                                            2: Gain Only
                                                            3: Gain Preferred
                                                         default value: 1
                                                    */
        OPTION_FRAMERATE = 0x11,       /* limit the frame rate, range=[0, 63], the default value 0 means no limit */
        OPTION_DEMOSAIC = 0x12,       /* demosaic method for both video and still image: BILINEAR = 0, VNG(Variable Number of Gradients) = 1, PPG(Patterned Pixel Grouping) = 2, AHD(Adaptive Homogeneity Directed) = 3, EA(Edge Aware) = 4, see https://en.wikipedia.org/wiki/Demosaicing, default value: 0 */
        OPTION_DEMOSAIC_VIDEO = 0x13,       /* demosaic method for video */
        OPTION_DEMOSAIC_STILL = 0x14,       /* demosaic method for still image */
        OPTION_BLACKLEVEL = 0x15,       /* black level */
        OPTION_MULTITHREAD = 0x16,       /* multithread image processing */
        OPTION_BINNING = 0x17,       /* binning
                                                           0x01: (no binning)
                                                           n: (saturating add, n*n), 0x02(2*2), 0x03(3*3), 0x04(4*4), 0x05(5*5), 0x06(6*6), 0x07(7*7), 0x08(8*8). The Bitdepth of the data remains unchanged.
                                                           0x40 | n: (unsaturated add in RAW mode, n*n), 0x42(2*2), 0x43(3*3), 0x44(4*4), 0x45(5*5), 0x46(6*6), 0x47(7*7), 0x48(8*8). The Bitdepth of the data is increased. For example, the original data with bitdepth of 12 will increase the bitdepth by 2 bits and become 14 after 2*2 binning.
                                                           0x80 | n: (average, n*n), 0x02(2*2), 0x03(3*3), 0x04(4*4), 0x05(5*5), 0x06(6*6), 0x07(7*7), 0x08(8*8). The Bitdepth of the data remains unchanged.
                                                       The final image size is rounded down to an even number, such as 640/3 to get 212
                                                    */
        OPTION_ROTATE = 0x18,       /* rotate clockwise: 0, 90, 180, 270 */
        OPTION_CG = 0x19,       /* Conversion Gain mode: 0 = LCG, 1 = HCG, 2 = HDR */
        OPTION_PIXEL_FORMAT = 0x1a,       /* pixel format */
        OPTION_FFC = 0x1b,       /* flat field correction
                                                        set:
                                                            0: disable
                                                            1: enable
                                                            -1: reset
                                                            (0xff000000 | n): set the average number to n, [1~255]
                                                        get:
                                                            (val & 0xff): 0 => disable, 1 => enable, 2 => inited
                                                            ((val & 0xff00) >> 8): sequence
                                                            ((val & 0xff0000) >> 16): average number
                                                    */
        OPTION_DDR_DEPTH = 0x1c,       /* the number of the frames that DDR can cache
                                                        1: DDR cache only one frame
                                                        0: Auto:
                                                            => one for video mode when auto exposure is enabled
                                                            => full capacity for others
                                                        1: DDR can cache frames to full capacity
                                                    */
        OPTION_DFC = 0x1d,       /* dark field correction
                                                        set:
                                                            0: disable
                                                            1: enable
                                                            -1: reset
                                                            (0xff000000 | n): set the average number to n, [1~255]
                                                        get:
                                                            (val & 0xff): 0 => disable, 1 => enable, 2 => inited
                                                            ((val & 0xff00) >> 8): sequence
                                                            ((val & 0xff0000) >> 16): average number
                                                    */
        OPTION_SHARPENING = 0x1e,       /* Sharpening: (threshold << 24) | (radius << 16) | strength)
                                                        strength: [0, 500], default: 0 (disable)
                                                        radius: [1, 10]
                                                        threshold: [0, 255]
                                                    */
        OPTION_FACTORY = 0x1f,       /* restore the factory settings */
        OPTION_TEC_VOLTAGE = 0x20,       /* get the current TEC voltage in 0.1V, 59 mean 5.9V; readonly */
        OPTION_TEC_VOLTAGE_MAX = 0x21,       /* TEC maximum voltage in 0.1V */
        OPTION_DEVICE_RESET = 0x22,       /* reset usb device, simulate a replug */
        OPTION_UPSIDE_DOWN = 0x23,       /* upsize down:
                                                        1: yes
                                                        0: no
                                                        default: 1 (win), 0 (linux/macos)
                                                    */
        OPTION_FOCUSPOS = 0x24,       /* focus positon */
        OPTION_AFMODE = 0x25,       /* auto focus mode (0:manul focus; 1:auto focus; 2:once focus; 3:conjugate calibration) */
        OPTION_AFZONE = 0x26,       /* auto focus zone */
        OPTION_AFFEEDBACK = 0x27,       /* auto focus information feedback; 0:unknown; 1:focused; 2:focusing; 3:defocus; 4:up; 5:down */
        OPTION_TESTPATTERN = 0x28,       /* test pattern:
                                                        0: TestPattern Off
                                                        3: monochrome diagonal stripes
                                                        5: monochrome vertical stripes
                                                        7: monochrome horizontal stripes
                                                        9: chromatic diagonal stripes
                                                    */
        OPTION_AUTOEXP_THRESHOLD = 0x29,       /* threshold of auto exposure, default value: 5, range = [2, 15] */
        OPTION_BYTEORDER = 0x2a,       /* Byte order, BGR or RGB: 0 => RGB, 1 => BGR, default value: 1(Win), 0(macOS, Linux, Android) */
        OPTION_NOPACKET_TIMEOUT = 0x2b,       /* no packet timeout: 0 = disable, positive value = timeout milliseconds. default: disable */
        OPTION_MAX_PRECISE_FRAMERATE = 0x2c,       /* get the precise frame rate maximum value in 0.1 fps, such as 115 means 11.5 fps. E_NOTIMPL means not supported */
        OPTION_PRECISE_FRAMERATE = 0x2d,       /* precise frame rate current value in 0.1 fps, range:[1~maximum] */
        OPTION_BANDWIDTH = 0x2e,       /* bandwidth, [1-100]% */
        OPTION_RELOAD = 0x2f,       /* reload the last frame in trigger mode */
        OPTION_CALLBACK_THREAD = 0x30,       /* dedicated thread for callback */
        OPTION_FRONTEND_DEQUE_LENGTH = 0x31,       /* frontend (raw) frame buffer deque length, range: [2, 1024], default: 4
                                                        All the memory will be pre-allocated when the camera starts, so, please attention to memory usage
                                                    */
        OPTION_FRAME_DEQUE_LENGTH = 0x31,       /* alias of TOUPCAM_OPTION_FRONTEND_DEQUE_LENGTH */
        OPTION_MIN_PRECISE_FRAMERATE = 0x32,       /* get the precise frame rate minimum value in 0.1 fps, such as 15 means 1.5 fps */
        OPTION_SEQUENCER_ONOFF = 0x33,       /* sequencer trigger: on/off */
        OPTION_SEQUENCER_NUMBER = 0x34,       /* sequencer trigger: number, range = [1, 255] */
        OPTION_SEQUENCER_EXPOTIME = 0x01000000, /* sequencer trigger: exposure time, iOption = OPTION_SEQUENCER_EXPOTIME | index, iValue = exposure time
                                                        For example, to set the exposure time of the third group to 50ms, call:
                                                           Toupcam_put_Option(TOUPCAM_OPTION_SEQUENCER_EXPOTIME | 3, 50000)
                                                    */
        OPTION_SEQUENCER_EXPOGAIN = 0x02000000, /* sequencer trigger: exposure gain, iOption = OPTION_SEQUENCER_EXPOGAIN | index, iValue = gain */
        OPTION_DENOISE = 0x35,       /* denoise, strength range: [0, 100], 0 means disable */
        OPTION_HEAT_MAX = 0x36,       /* get maximum level: heat to prevent fogging up */
        OPTION_HEAT = 0x37,       /* heat to prevent fogging up */
        OPTION_LOW_NOISE = 0x38,       /* low noise mode (Higher signal noise ratio, lower frame rate): 1 => enable */
        OPTION_POWER = 0x39,       /* get power consumption, unit: milliwatt */
        OPTION_GLOBAL_RESET_MODE = 0x3a,       /* global reset mode */
        OPTION_OPEN_USB_ERRORCODE = 0x3b,       /* get the open usb error code */
        OPTION_FLUSH = 0x3d,        /* 1 = hard flush, discard frames cached by camera DDR (if any)
                                                        2 = soft flush, discard frames cached by toupcam.dll (if any)
                                                        3 = both flush
                                                        Toupcam_Flush means 'both flush'
                                                        return the number of soft flushed frames if successful, HRESULT if failed
                                                     */
        OPTION_NUMBER_DROP_FRAME = 0x3e,        /* get the number of frames that have been grabbed from the USB but dropped by the software */
        OPTION_DUMP_CFG = 0x3f,        /* 0 = when camera is stopped, do not dump configuration automatically
                                                        1 = when camera is stopped, dump configuration automatically
                                                        -1 = explicitly dump configuration once
                                                        default: 1
                                                     */
        OPTION_DEFECT_PIXEL = 0x40,        /* Defect Pixel Correction: 0 => disable, 1 => enable; default: 1 */
        OPTION_BACKEND_DEQUE_LENGTH = 0x41,        /* backend (pipelined) frame buffer deque length (Only available in pull mode), range: [2, 1024], default: 3
                                                        All the memory will be pre-allocated when the camera starts, so, please attention to memory usage
                                                     */
        OPTION_LIGHTSOURCE_MAX = 0x42,        /* get the light source range, [0 ~ max] */
        OPTION_LIGHTSOURCE = 0x43,        /* light source */
        OPTION_HEARTBEAT = 0x44,        /* Heartbeat interval in millisecond, range = [HEARTBEAT_MIN, HEARTBEAT_MAX], 0 = disable, default: disable */
        OPTION_FRONTEND_DEQUE_CURRENT = 0x45,        /* get the current number in frontend deque */
        OPTION_BACKEND_DEQUE_CURRENT = 0x46,        /* get the current number in backend deque */
        OPTION_EVENT_HARDWARE = 0x04000000,  /* enable or disable hardware event: 0 => disable, 1 => enable; default: disable
                                                            (1) iOption = TOUPCAM_OPTION_EVENT_HARDWARE, master switch for notification of all hardware events
                                                            (2) iOption = TOUPCAM_OPTION_EVENT_HARDWARE | (event type), a specific type of sub-switch
                                                        Only if both the master switch and the sub-switch of a particular type remain on are actually enabled for that type of event notification.
                                                     */
        OPTION_PACKET_NUMBER = 0x47,        /* get the received packet number */
        OPTION_FILTERWHEEL_SLOT = 0x48,        /* filter wheel slot number */
        OPTION_FILTERWHEEL_POSITION = 0x49,        /* filter wheel position:
                                                             set:
                                                                 -1: calibrate
                                                                 val & 0xff: position between 0 and N-1, where N is the number of filter slots
                                                                 (val >> 8) & 0x1: direction, 0 => clockwise spinning, 1 => auto direction spinning
                                                             get:
                                                                -1: in motion
                                                                val: position arrived
                                                     */
        OPTION_AUTOEXPOSURE_PERCENT = 0x4a,        /* auto exposure percent to average:
                                                             1~99: peak percent average
                                                             0 or 100: full roi average
                                                     */
        OPTION_ANTI_SHUTTER_EFFECT = 0x4b,        /* anti shutter effect: 1 => disable, 0 => disable; default: 1 */
        OPTION_CHAMBER_HT = 0x4c,        /* get chamber humidity & temperature:
                                                             high 16 bits: humidity, in 0.1%, such as: 325 means humidity is 32.5%
                                                             low 16 bits: temperature, in 0.1 degrees Celsius, such as: 32 means 3.2 degrees Celsius
                                                     */
        OPTION_ENV_HT = 0x4d,        /* get environment humidity & temperature */
        OPTION_EXPOSURE_PRE_DELAY = 0x4e,        /* exposure signal pre-delay, microsecond */
        OPTION_EXPOSURE_POST_DELAY = 0x4f,        /* exposure signal post-delay, microsecond */
        OPTION_AUTOEXPO_CONV = 0x50,        /* get auto exposure convergence status: 1(YES) or 0(NO), -1(NA) */
        OPTION_AUTOEXPO_TRIGGER = 0x51,        /* auto exposure on trigger mode: 0 => disable, 1 => enable; default: 0 */
        OPTION_LINE_PRE_DELAY = 0x52,        /* specified line signal pre-delay, microsecond */
        OPTION_LINE_POST_DELAY = 0x53,        /* specified line signal post-delay, microsecond */
        OPTION_TEC_VOLTAGE_MAX_RANGE = 0x54,        /* get the tec maximum voltage range:
                                                             high 16 bits: max
                                                             low 16 bits: min
                                                     */
        OPTION_HIGH_FULLWELL = 0x55         /* high fullwell capacity: 0 => disable, 1 => enable */
    };
}