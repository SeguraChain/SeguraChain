using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SeguraChain_Desktop_Wallet.Common;
using SeguraChain_Desktop_Wallet.Language.Enum;
using SeguraChain_Desktop_Wallet.Language.Object;
using SeguraChain_Desktop_Wallet.Settings.Enum;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Desktop_Wallet.Language.Database
{
    public class ClassLanguageDatabase
    {
        private string _currentLanguage;
        private Dictionary<string, ClassLanguageObject> _dictionaryLanguageObjects;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassLanguageDatabase()
        {
            _dictionaryLanguageObjects = new Dictionary<string, ClassLanguageObject>();
        }

        /// <summary>
        /// Load language files and push them into the database.
        /// </summary>
        /// <returns></returns>
        public bool LoadLanguageDatabase(bool withoutWalletSetting)
        {
            string languageDirectoryPath = ClassUtility.ConvertPath(AppContext.BaseDirectory + ClassWalletDefaultSetting.DefaultLanguageDirectoryFilePath);

            if (!Directory.Exists(languageDirectoryPath))
                Directory.CreateDirectory(languageDirectoryPath);

            string[] languageFileList = Directory.GetFiles(languageDirectoryPath, ClassWalletDefaultSetting.LanguageFileFormat);

            if (languageFileList.Length == 0)
                InitializeDefaultLanguage(languageDirectoryPath);
            else
            {
                foreach (var languageFilePath in languageFileList)
                {

                    using (StreamReader reader = new StreamReader(languageFilePath))
                    {
                        bool readStatus = false;
                        if (ClassUtility.TryDeserialize(reader.ReadToEnd(), out ClassLanguageObject languageObject, ObjectCreationHandling.Reuse))
                        {
                            if (languageObject != null)
                            {
                                if (!languageObject.LanguageName.IsNullOrEmpty(out _) && !languageObject.LanguageMinName.IsNullOrEmpty(out _))
                                {
                                    readStatus = true;
                                    if (!_dictionaryLanguageObjects.ContainsKey(languageObject.LanguageMinName))
                                    {
                                        _dictionaryLanguageObjects.Add(languageObject.LanguageMinName, languageObject);

                                        ClassLog.WriteLine("Language file: " + languageFilePath + " read sucessfully done. Language Name: " + languageObject.LanguageMinName, ClassEnumLogLevelType.LOG_LEVEL_WALLET, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                                    }
                                }
                            }
                        }

                        if (!readStatus)
                            ClassLog.WriteLine("Language file: " + languageFilePath + " reading failed", ClassEnumLogLevelType.LOG_LEVEL_WALLET, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                    }
                }

                if (!withoutWalletSetting)
                {
                    if (_dictionaryLanguageObjects.Count == 0)
                        InitializeDefaultLanguage(languageDirectoryPath);
                    else
                    {
                        if (_dictionaryLanguageObjects.ContainsKey(ClassDesktopWalletCommonData.WalletSettingObject.WalletLanguageNameSelected))
                            _currentLanguage = ClassDesktopWalletCommonData.WalletSettingObject.WalletLanguageNameSelected;
                        else
                            InitializeDefaultLanguage(languageDirectoryPath);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Initialize the default language.
        /// </summary>
        /// <param name="languageDirectoryPath"></param>
        private void InitializeDefaultLanguage(string languageDirectoryPath)
        {
            ClassLanguageObject defaultLanguageObject = new ClassLanguageObject();
            _currentLanguage = defaultLanguageObject.LanguageMinName;
            if (!_dictionaryLanguageObjects.ContainsKey(_currentLanguage))
            {
                _dictionaryLanguageObjects.Add(defaultLanguageObject.LanguageMinName, defaultLanguageObject);

                using (StreamWriter writer = new StreamWriter(languageDirectoryPath + defaultLanguageObject.LanguageName + ClassWalletDefaultSetting.LanguageFileFormat.Replace("*", "")))
                {
                    writer.Write(ClassUtility.SerializeData(defaultLanguageObject, Formatting.Indented));
                }
            }
        }

        /// <summary>
        /// Get the language content object depending of the type selected.
        /// </summary>
        /// <typeparam name="T">Type of language object data.</typeparam>
        /// <param name="languageType">Language type target.</param>
        /// <returns></returns>
        public T GetLanguageContentObject<T>(ClassLanguageEnumType languageType)
        {
            switch (languageType)
            {
                case ClassLanguageEnumType.LANGUAGE_TYPE_STARTUP_FORM:
                    {
                        return (T)Convert.ChangeType(_dictionaryLanguageObjects[_currentLanguage].WalletStartupFormLanguage, typeof(T));
                    }
                case ClassLanguageEnumType.LANGUAGE_TYPE_MAIN_FORM:
                    {
                        return (T)Convert.ChangeType(_dictionaryLanguageObjects[_currentLanguage].WalletMainFormLanguage, typeof(T));
                    }
                case ClassLanguageEnumType.LANGUAGE_TYPE_CREATE_WALLET_FORM:
                    {
                        return (T)Convert.ChangeType(_dictionaryLanguageObjects[_currentLanguage].WalletCreateFormLanguage, typeof(T));
                    }
                case ClassLanguageEnumType.LANGUAGE_TYPE_WALLET_RESCAN_FORM:
                    {
                        return (T)Convert.ChangeType(_dictionaryLanguageObjects[_currentLanguage].WalletRescanFormLanguage, typeof(T));
                    }
                case ClassLanguageEnumType.LANGUAGE_TYPE_TRANSACTION_HISTORY_INFORMATION_FORM:
                    {
                        return (T)Convert.ChangeType(_dictionaryLanguageObjects[_currentLanguage].WalletTransactionHistoryInformationFormLanguage, typeof(T));
                    }
                case ClassLanguageEnumType.LANGUAGE_TYPE_SEND_TRANSACTION_PASSPHRASE_FORM:
                    {
                        return (T)Convert.ChangeType(_dictionaryLanguageObjects[_currentLanguage].WalletSendTransactionPassphraseFormLanguage, typeof(T));
                    }
                case ClassLanguageEnumType.LANGUAGE_TYPE_SEND_TRANSACTION_CONFIRMATION_FORM:
                    {
                        return (T)Convert.ChangeType(_dictionaryLanguageObjects[_currentLanguage].WalletSendTransactionConfirmationFormLanguage, typeof(T));
                    }
                case ClassLanguageEnumType.LANGUAGE_TYPE_SEND_TRANSACTION_WAIT_REQUEST_FORM:
                    {
                        return (T)Convert.ChangeType(_dictionaryLanguageObjects[_currentLanguage].WalletSendTransactionWaitRequestFormLanguage, typeof(T));
                    }
                case ClassLanguageEnumType.LANGUAGE_TYPE_IMPORT_WALLET_PRIVATE_KEY_FORM:
                    {
                        return (T)Convert.ChangeType(_dictionaryLanguageObjects[_currentLanguage].WalletImportPrivateKeyFormLanguage, typeof(T));
                    }
                case ClassLanguageEnumType.LANGUAGE_TYPE_WALLET_SETUP_FORM:
                    {
                        return (T)Convert.ChangeType(_dictionaryLanguageObjects[_currentLanguage].WalletSetupFormLanguage, typeof(T));
                    }
                case ClassLanguageEnumType.LANGUAGE_TYPE_WALLET_SETUP_STEP_ONE_FORM:
                    {
                        return (T)Convert.ChangeType(_dictionaryLanguageObjects[_currentLanguage].WalletSetupStepOneFormLanguage, typeof(T));
                    }
                case ClassLanguageEnumType.LANGUAGE_TYPE_WALLET_SETUP_STEP_FINAL_FORM:
                    {
                        return (T)Convert.ChangeType(_dictionaryLanguageObjects[_currentLanguage].WalletSetupStepFinalFormLanguage, typeof(T));
                    }
                case ClassLanguageEnumType.LANGUAGE_TYPE_TRANSACTION_HISTORY_INFORMATION_LOADING_FORM:
                    {
                        return (T)Convert.ChangeType(_dictionaryLanguageObjects[_currentLanguage].WalletTransactionHistoryInformationLoadingFormLanguage, typeof(T));
                    }
            }

            return default(T);
        }

        /// <summary>
        /// Get the language list names.
        /// </summary>
        public Dictionary<string, string> GetLanguageList
        {
            get
            {
                Dictionary<string, string> languageList = new Dictionary<string, string>();

                foreach (var languageObject in _dictionaryLanguageObjects)
                    if (!languageList.ContainsKey(languageObject.Key))
                        languageList.Add(languageObject.Key, languageObject.Value.LanguageName);

                return languageList;
            }
        }



        /// <summary>
        /// Change the new language name.
        /// </summary>
        /// <param name="languageMinName"></param>
        public bool SetCurrentLanguageName(string languageMinName)
        {
            if (_dictionaryLanguageObjects.ContainsKey(languageMinName))
            {
                _currentLanguage = languageMinName;
                ClassDesktopWalletCommonData.WalletSettingObject.WalletLanguageNameSelected = languageMinName;
                return true;
            }
            return false;
        }
    }
}
