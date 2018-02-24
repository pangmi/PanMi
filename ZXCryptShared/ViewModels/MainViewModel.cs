﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.ComponentModel;
using System.Windows.Input;
using System.Security;
using System.Security.Cryptography;
using System.Windows.Controls;
using System.Diagnostics;
using Microsoft.Win32;

using MahApps.Metro.Controls.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs;
using ZXEncryption;

namespace ZXCryptShared
{
    public class MainViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        public MainViewModel()
        {
        }

        private const string _encryptedFileExt = ".pxx";
        private IDialogCoordinator _dialogCoordinator;

        // Constructor
        public MainViewModel(IDialogCoordinator instance)
        {
            _dialogCoordinator = instance;
        }

        #region Properties

        public string AppName => "PanMi";

        public string AppVersion
        {
            get
            {
                var ver = Assembly.GetExecutingAssembly().GetName().Version;
                string result = String.Format("{0}.{1}.{2}", ver.Major, ver.Minor, ver.Build);
                return result;
            }
        }

        public IEnumerable<string> FileList { get; set; }

        public EncryptionMode Mode { get; set; }

        public string DisplayFileName
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (string file in FileList)
                {
                    sb.Append(Path.GetFileName(file));
                    sb.Append("; ");
                }

                string filenames = sb.ToString().TrimEnd(new char[] { ';', ' ' });
                return filenames;
            }
        }

        private bool _deleteOldFile = true;
        public bool DeleteOldFile
        {
            get { return _deleteOldFile; }
            set
            {
                if (!Equals(value, _deleteOldFile))
                {
                    _deleteOldFile = value;
                    RaisePropertyChanged("DeleteOldFile");
                }
            }
        }

        public SecureString Password1
        {
            private get;
            set;
        }

        public SecureString Password2
        {
            private get;
            set;
        }

        private string _encryptionKey;
        public string EncryptionKey
        {
            get { return _encryptionKey; }
            set
            {
                if (!Equals(value, _encryptionKey))
                {
                    _encryptionKey = value;
                    RaisePropertyChanged("EncryptionKey");
                }
            }
        }

        private string _keyFile;
        public string KeyFile
        {
            get { return _keyFile; }
            set
            {
                if (!Equals(value, _keyFile))
                {
                    _keyFile = value;
                    RaisePropertyChanged("KeyFile");
                }
            }
        }

        private string _inputFile;
        public string InputFile
        {
            get { return _inputFile; }
            set
            {
                if (!Equals(value, _inputFile))
                {
                    _inputFile = value;
                    RaisePropertyChanged("InputFile");
                }
            }
        }

        private string _outputDir;
        public string OutputDir
        {
            get { return _outputDir; }
            set
            {
                if (!Equals(value, _outputDir))
                {
                    _outputDir = value;
                    RaisePropertyChanged("OutputDir");
                }
            }
        }

        private bool _isEncryption = true;
        public bool IsEncryption
        {
            get { return _isEncryption; }
            set
            {
                this._isEncryption = value;
                RaisePropertyChanged("IsEncryption");
            }
        }

        private bool _overwriteExistingFile = true;
        public bool OverwriteExistingFile
        {
            get { return _overwriteExistingFile; }
            set
            {
                _overwriteExistingFile = value;
                RaisePropertyChanged("OverwriteExistingFile");
            }
        }

        private bool _closeWindow;
        /// <summary>
        /// Indicate if the main window should be closed
        /// </summary>
        public bool CloseWindow
        {
            get { return this._closeWindow; }
            set
            {
                this._closeWindow = value;
                RaisePropertyChanged("CloseWindow");
            }
        }

        #endregion

        #region INotifyPropertyChanged methods

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event if needed.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region IDataErrorInfo methods

        public string this[string columnName]
        {
            get
            {
                return String.Empty;
            }
        }

        public string Error
        {
            get
            {
                return String.Empty;
            }
        }

        #endregion

        #region Event handles

        private ICommand _textBtnCmdGenerateKey;
        public ICommand TextBtnCmdGenerateKey => this._textBtnCmdGenerateKey ?? (this._textBtnCmdGenerateKey = new SimpleCommand
        {
            CanExecuteDelegate = x => true,
            ExecuteDelegate = async x =>
            {
                try
                {
                    using (SymmetricAlgorithm algorithm = CreateAlgorithm(typeof(AesCryptoServiceProvider)))
                    {
                        algorithm.GenerateKey();
                        byte[] generatedKey = algorithm.Key;
                        EncryptionKey = Utility.ByteArrayToHexString(generatedKey);
                    }
                }
                catch (Exception ex)
                {
                    if (_dialogCoordinator != null)
                        await _dialogCoordinator.ShowMessageAsync(this, "Exception!", ex.Message);
                }
            }
        });

        private ICommand _textBtnCmdSelectKeyFile;
        public ICommand TextBtnCmdSelectKeyFile => this._textBtnCmdSelectKeyFile ?? (this._textBtnCmdSelectKeyFile = new SimpleCommand
        {
            CanExecuteDelegate = x => true,
            ExecuteDelegate = async x =>
            {
                try
                {
                    var dlg = new OpenFileDialog
                    {
                        Title = "Open key file",
                        FileName = "EncryptionKey",
                        DefaultExt = ".txt",
                        CheckFileExists = true,
                        Filter = "Key File|*.txt|All Files|*.*"
                    };

                    if (dlg.ShowDialog() == true)
                        KeyFile = dlg.FileName;
                }
                catch (Exception ex)
                {
                    if (_dialogCoordinator != null)
                        await _dialogCoordinator.ShowMessageAsync(this, "Exception!", ex.Message);
                }
            }
        });

        private ICommand _btnCmdSaveKeyFile;
        public ICommand BtnCmdSaveKeyFile => _btnCmdSaveKeyFile ?? (_btnCmdSaveKeyFile = new SimpleCommand
        {
            CanExecuteDelegate = x =>
            {
                if (String.IsNullOrWhiteSpace(EncryptionKey))
                    return false;

                return true;
            },

            ExecuteDelegate = async x =>
            {
                try
                {
                    var dlg = new SaveFileDialog
                    {
                        Title = "Save key to a file",
                        FileName = "EncryptionKey",
                        DefaultExt = ".txt",
                        Filter = "Key File|*.txt|All Files|*.*"
                    };

                    if (dlg.ShowDialog() == true)
                    {
                        XDocument doc = new XDocument(new XElement("encryptionKey",
                                                                    new XElement("version", AppVersion),
                                                                    new XElement("key", EncryptionKey)
                                                                    ));
                        doc.Save(dlg.FileName);
                        await _dialogCoordinator.ShowMessageAsync(this, title: "Key file saved", message: dlg.FileName);
                    }
                }
                catch (Exception ex)
                {
                    if (_dialogCoordinator != null)
                        await _dialogCoordinator.ShowMessageAsync(this, title: "Exception!", message: ex.Message);
                }
            }
        });

        private ICommand _pwbCmdPassword1Changed;
        public ICommand PwbCmdPassword1Changed => _pwbCmdPassword1Changed ?? (_pwbCmdPassword1Changed = new SimpleCommand
        {
            CanExecuteDelegate = x => true,
            ExecuteDelegate =  x =>
            {
                if (x != null && x is PasswordBox)
                {
                    Password1 = ((PasswordBox)x).SecurePassword;
                }
            }
        });

        private ICommand _pwbCmdPassword2Changed;
        public ICommand PwbCmdPassword2Changed => _pwbCmdPassword2Changed ?? (_pwbCmdPassword2Changed = new SimpleCommand
        {
            CanExecuteDelegate = x => true,
            ExecuteDelegate = x =>
            {
                if (x != null && x is PasswordBox)
                {
                    Password2 = ((PasswordBox)x).SecurePassword;
                }
            }
        });

        private ICommand _btnCmdOpenEncDecFile;
        public ICommand BtnCmdOpenEncDecFile
        {
            get
            {
                return this._btnCmdOpenEncDecFile ?? (this._btnCmdOpenEncDecFile = new SimpleCommand
                {
                    CanExecuteDelegate = x =>
                    {
                        if (Password1 == null || Password1.Length <= 0)
                            return false;

                        if (Mode == EncryptionMode.Encrypt && (Password2 == null || Password2.Length <= 0))
                            return false;

                        return true;
                    },

                    ExecuteDelegate = async x =>
                    {
                        byte[] passcode = null;
                        byte[] key = null;
                        try
                        {
                            if (Mode == EncryptionMode.Encrypt)
                            {
                                if (!CompareSecureString(Password1, Password2))
                                {
                                    await _dialogCoordinator.ShowMessageAsync(this, title: "Password mismatch", message: "Please try again.");
                                    return;
                                }
                            }

                            using (SecureStringWrapper wrapper = new SecureStringWrapper(Password1))
                            {
                                if (!String.IsNullOrWhiteSpace(KeyFile))
                                {
                                    key = GetEncryptionKey(KeyFile);
                                }

                                //string pwdStr = wrapper.ClearText;
                                //byte[] pwdBytes = wrapper.ByteArray;

                                passcode = MergeBytes(wrapper.ByteArray, key);
                            }

                            // open/encrypt/decrypt file
                            IZXCryptor cryptor = new ZXCryptor();
                            foreach (string file in FileList)
                            {
                                if (Mode == EncryptionMode.Encrypt)
                                {
                                    string outfile = file + _encryptedFileExt;
                                    try
                                    {
                                        cryptor.EncryptFile(file, outfile, passcode);
                                    }
                                    catch
                                    {
                                        if (File.Exists(outfile))  { File.Delete(outfile); }
                                        throw;
                                    }

                                    if (DeleteOldFile)
                                    {
                                        FileInfo fi = new FileInfo(file);
                                        fi.IsReadOnly = false;

                                        // permanent delete is not possible, see https://stackoverflow.com/questions/38935535/is-overwriting-a-file-multiple-times-enough-to-erase-its-data
                                        File.Delete(file);
                                    }
                                }
                                else if (Mode == EncryptionMode.Decrypt)
                                {
                                    // check file extension
                                    if (!Equals(Path.GetExtension(file), _encryptedFileExt))
                                        continue;

                                    string outfile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));
                                    try
                                    {
                                        cryptor.DecryptFile(file, outfile, passcode);
                                    }
                                    catch
                                    {
                                        if (File.Exists(outfile)) { File.Delete(outfile); }
                                        throw;
                                    }

                                    if (DeleteOldFile)
                                    {
                                        FileInfo fi = new FileInfo(file);
                                        fi.IsReadOnly = false;
                                        File.Delete(file);
                                    }
                                }
                                else if (Mode == EncryptionMode.Open)
                                {
                                    // check file extension
                                    if (!Equals(Path.GetExtension(file), _encryptedFileExt))
                                        break;

                                    string tmpFolder = Path.GetTempPath() + "zxcrypt\\";
                                    tmpFolder += Path.GetRandomFileName().Replace(".", String.Empty);
                                    string outfile = Path.Combine(tmpFolder, Path.GetFileNameWithoutExtension(file));
                                    try
                                    {
                                        cryptor.DecryptFile(file, outfile, passcode);

                                        // open file
                                        Process.Start(outfile);

                                        // launch ZXCryptMon, which is one instance app,  to clean up the temp files once the above process close
                                        string dllPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
                                        string appFullPath = Path.GetDirectoryName(dllPath) + "\\ZXCryptMon.exe";
                                        Process.Start(appFullPath);

                                        // for open, we will handle only one file
                                        break;
                                    }
                                    catch
                                    {
                                        Directory.Delete(tmpFolder, true);
                                        throw;
                                    }
                                }
                            }

                            // clear password and key
                            if (passcode != null)
                            {
                                for (int i = 0; i < passcode.Length; i++)
                                    passcode[i] = 0;
                            }
                            if (key != null)
                            {
                                for (int i = 0; i < key.Length; i++)
                                    key[i] = 0;
                            }

                            // everything is good, so close the window
                            CloseWindow = true;
                        }
                        catch (Exception ex)
                        {
                            // clear password and key
                            if (passcode != null)
                            {
                                for (int i = 0; i < passcode.Length; i++)
                                    passcode[i] = 0;
                            }
                            if (key != null)
                            {
                                for (int i = 0; i < key.Length; i++)
                                    key[i] = 0;
                            }

                            if (_dialogCoordinator != null)
                                await _dialogCoordinator.ShowMessageAsync(this, title: "Exception!", message: ex.Message);
                        }
                    }
                });
            }
        }

        private ICommand _textCmdInputFileChanged;
        public ICommand TextCmdInputFileChanged => _textCmdInputFileChanged ?? (_textCmdInputFileChanged = new SimpleCommand
        {
            CanExecuteDelegate = x => true,
            ExecuteDelegate = x =>
            {
                if (!Equals(Path.GetExtension(InputFile), _encryptedFileExt))
                    IsEncryption = true;
                else
                    IsEncryption = false;
            }
        });

        private ICommand _textBtnCmdSelectInputFile;
        public ICommand TextBtnCmdSelectInputFile => this._textBtnCmdSelectInputFile ?? (this._textBtnCmdSelectInputFile = new SimpleCommand
        {
            CanExecuteDelegate = x => true,
            ExecuteDelegate = async x =>
            {
                try
                {
                    var dlg = new OpenFileDialog
                    {
                        Title = "Select input file",
                        CheckFileExists = true,
                        Filter = "All Files|*.*"
                    };

                    if (dlg.ShowDialog() == true)
                    {
                        InputFile = dlg.FileName;
                        OutputDir = Path.GetDirectoryName(InputFile);
                    }
                }
                catch (Exception ex)
                {
                    if (_dialogCoordinator != null)
                        await _dialogCoordinator.ShowMessageAsync(this, "Exception!", ex.Message);
                }
            }
        });

        private ICommand _textBtnCmdSelectOutputDir;
        public ICommand TextBtnCmdSelectOutputDir => _textBtnCmdSelectOutputDir ?? (_textBtnCmdSelectOutputDir = new SimpleCommand
        {
            CanExecuteDelegate = x => true,
            ExecuteDelegate = async x =>
            {
                try
                {
                    var dlg = new CommonOpenFileDialog
                    {
                        Title = "Select output folder",
                        IsFolderPicker = true,
                        InitialDirectory = Path.GetDirectoryName(InputFile),

                        AddToMostRecentlyUsedList = false,
                        AllowNonFileSystemItems = false,
                        DefaultDirectory = Path.GetDirectoryName(InputFile),
                        EnsurePathExists = true,
                        EnsureReadOnly = false,
                        EnsureValidNames = true,
                        Multiselect = false,
                        ShowPlacesList = true
                    };

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        OutputDir = dlg.FileName;
                    }
                }
                catch (Exception ex)
                {
                    if (_dialogCoordinator != null)
                        await _dialogCoordinator.ShowMessageAsync(this, "Exception!", ex.Message);
                }
            }
        });

        private ICommand _btnCmdCryptFile;
        public ICommand BtnCmdCryptFile => _btnCmdCryptFile ?? (_btnCmdCryptFile = new SimpleCommand
        {
            CanExecuteDelegate = x =>
            {
                if (String.IsNullOrWhiteSpace(InputFile))
                    return false;

                if (String.IsNullOrWhiteSpace(OutputDir))
                    return false;

                if (Password1 == null || Password1.Length <= 0)
                    return false;

                if (IsEncryption && (Password2 == null || Password2.Length <= 0))
                    return false;

                return true;
            },

            ExecuteDelegate = async x =>
            {
                byte[] passcode = null;
                byte[] key = null;
                try
                {
                    if (!File.Exists(InputFile))
                    {
                        await _dialogCoordinator.ShowMessageAsync(this, title: "Error", message: "Input file does not exist.");
                        return;
                    }

                    if (!Directory.Exists(OutputDir))
                    {
                        await _dialogCoordinator.ShowMessageAsync(this, title: "Error", message: "Output directory does not exist.");
                        return;
                    }

                    string outputFileName = IsEncryption ? Path.GetFileName(InputFile) + _encryptedFileExt : Path.GetFileNameWithoutExtension(InputFile);
                    string outputFilePath = Path.Combine(OutputDir, outputFileName);
                    if (!OverwriteExistingFile && File.Exists(outputFilePath))
                    {
                        var dlgSetting = new MetroDialogSettings()
                        {
                            AffirmativeButtonText = "Yes",
                            NegativeButtonText = "No"
                        };

                        string msg = String.Format("Output file '{0}' already exists. Do you want to overwrite it?", outputFileName);
                        MessageDialogResult result = await _dialogCoordinator.ShowMessageAsync(this, "Overwrite File",msg, MessageDialogStyle.AffirmativeAndNegative, dlgSetting);
                        if (result != MessageDialogResult.Affirmative)
                            return;
                    }

                    if (IsEncryption)
                    {
                        if (!CompareSecureString(Password1, Password2))
                        {
                            await _dialogCoordinator.ShowMessageAsync(this, title: "Password mismatch", message: "Please try again.");
                            return;
                        }
                    }

                    using (SecureStringWrapper wrapper = new SecureStringWrapper(Password1))
                    {
                        if (!String.IsNullOrWhiteSpace(KeyFile))
                        {
                            key = GetEncryptionKey(KeyFile);
                        }

                        passcode = MergeBytes(wrapper.ByteArray, key);
                    }

                    // encrypt/decrypt file
                    IZXCryptor cryptor = new ZXCryptor();
                    if (IsEncryption)
                    {
                        cryptor.EncryptFile(InputFile, outputFilePath, passcode);
                    }
                    else
                    {
                        cryptor.DecryptFile(InputFile, outputFilePath, passcode);
                    }

                    // clear password and key
                    if (passcode != null)
                    {
                        for (int i = 0; i < passcode.Length; i++)
                            passcode[i] = 0;
                    }
                    if (key != null)
                    {
                        for (int i = 0; i < key.Length; i++)
                            key[i] = 0;
                    }

                    if (IsEncryption)
                    {
                        await _dialogCoordinator.ShowMessageAsync(this, title: "File encrypted", message: "Output file: " + outputFilePath);
                    }
                    else
                    {
                        await _dialogCoordinator.ShowMessageAsync(this, title: "File decrypted", message: "Output file: " + outputFilePath);
                    }
                }
                catch (Exception ex)
                {
                    // clear password and key
                    if (passcode != null)
                    {
                        for (int i = 0; i < passcode.Length; i++)
                            passcode[i] = 0;
                    }
                    if (key != null)
                    {
                        for (int i = 0; i < key.Length; i++)
                            key[i] = 0;
                    }

                    if (_dialogCoordinator != null)
                        await _dialogCoordinator.ShowMessageAsync(this, title: "Exception!", message: ex.Message);
                }
            }
        });

        #endregion

        private SymmetricAlgorithm CreateAlgorithm(Type symmetricAlgorithmType)
        {
            SymmetricAlgorithm algorithm = null;
            try
            {
                algorithm = (SymmetricAlgorithm)Activator.CreateInstance(symmetricAlgorithmType);
            }
            catch (Exception)
            {
                throw;
            }
            return algorithm;
        }

        private bool CompareSecureString(SecureString strA, SecureString strB)
        {
            using (SecureStringWrapper wrapperA = new SecureStringWrapper(strA))
            {
                using (SecureStringWrapper wrapperB = new SecureStringWrapper(strB))
                {
                    if (Equals(wrapperA.ClearText, wrapperB.ClearText))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private byte[] GetEncryptionKey(string keyFile)
        {
            if (String.IsNullOrWhiteSpace(keyFile))
                return null;

            if (!File.Exists(keyFile))
            {
                throw new FileNotFoundException("Key file does not exist.");
            }

            try
            {
                XDocument doc = XDocument.Load(keyFile);
                XElement node = doc.Descendants("key").FirstOrDefault();
                return Utility.HexStringToByteArray(node.Value);
            }
            catch (Exception ex)
            {
                throw new System.Xml.XmlException("Invalid key file.", ex);
            }
        }

        private byte[] MergeBytes(byte[] a, byte[] b)
        {
            int lena = (a == null) ? 0 : a.Length;
            int lenb = (b == null) ? 0 : b.Length;
            if ((lena + lenb) == 0)
                return null;

            byte[] output = new byte[lena + lenb];
            for (int i = 0; i < lena; i++)
                output[i] = a[i];
            for (int j = 0; j < lenb; j++)
                output[lena + j] = b[j];
            return output;
        }
    }
}
