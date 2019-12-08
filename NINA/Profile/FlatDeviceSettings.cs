﻿using System;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    internal class FlatDeviceSettings : Settings, IFlatDeviceSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            Id = "No_Device";
        }

        private string _id;

        [DataMember]
        public string Id {
            get => _id;
            set {
                if (_id == value) return;
                _id = value;
                RaisePropertyChanged();
            }
        }

        private string _name;

        [DataMember]
        public string Name {
            get => _name;
            set {
                if (_name == value) return;
                _name = value;
                RaisePropertyChanged();
            }
        }

        private string _portName;

        [DataMember]
        public string PortName {
            get => _portName;
            set {
                if (_portName == value) return;
                _portName = value;
                RaisePropertyChanged();
            }
        }

        private bool _closeAtSequenceEnd;

        [DataMember]
        public bool CloseAtSequenceEnd {
            get => _closeAtSequenceEnd;
            set {
                if (_closeAtSequenceEnd == value) return;
                _closeAtSequenceEnd = value;
                RaisePropertyChanged();
            }
        }

        private bool _openForDarkFlats;

        [DataMember]
        public bool OpenForDarkFlats {
            get => _openForDarkFlats;
            set {
                if (_openForDarkFlats == value) return;
                _openForDarkFlats = value;
                RaisePropertyChanged();
            }
        }
    }
}