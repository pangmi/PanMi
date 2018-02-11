# PanMi Tools
PanMi Tools is a set of C# projects which build Windows shell extension and application for file encryption and decryption with AES. You can encrypt or decrypt a file with the application, or through a Windows Explorer context menu.

### Projects 
- ZXEncryption: Cryptography methods 
- ZXCryptApp: WPF application for file encryption/decryption
- ZXCryptShared: UI and file encryption/decryption functionalities
- ZXCryptShellExtension: Windows shell extension with context menus for file encryption/decryption
- ZXCryptMon: Monitor and clean up temp files
- ZXCryptInstall: Package installer project

### Usage
You can download and build the solution with Visual Studion 15/17, or download the install package PanMiCryptInstall.msi from the release. After install, you can run file encryption/decryption thru Windows conetext menu, or run the standalone application.

#### Context Menu
By default, the encrypted file has an extension of ".pxx". Double-click on a encrypted file will launch an Open dialog. Once a valid password provided, the original file will be opened.

#### Standalone Application

### Credit
The following open source projects are used:
- [SharpSHell](https://github.com/dwmkerr/sharpshell)
- [MahApps.Metro](https://github.com/MahApps/MahApps.Metro)
