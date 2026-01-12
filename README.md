#The Hidden Text Detector

<a href="https://doi.org/10.5281/zenodo.18217716"><img src="https://zenodo.org/badge/1132460025.svg" alt="DOI"></a>
 
Hidden Text Detector (HTD) is a Windows application designed to identify and reveal concealed text in PDF, Word, and Excel documents. This tool is essential for document analysis, compliance verification, and security auditing.
The application detects hidden content using advanced heuristics, including microfonts (under 4pt), white or near-white text, and documents with hidden text attributes. This helps users uncover potentially concealed information that may not be immediately visible.

Use Cases: Document forensics, compliance auditing, intellectual property protection, and transparency verification. 
This tool is intended for legitimate purposes such as security research, document verification, and compliance checking. Users are responsible for ensuring they have the proper authorisation to analyse documents.

Key Features:
• Python-based backend analyser (PyMuPDF, python-docx, openpyxl)
• C# WPF frontend with Pigeon Blue theme
• Advanced filtering and search capabilities
• CSV export with proper formatting
• Impressum dialogue box with copyright
• Statistics Panel showing hidden text breakdown

Technology Stack:
•	Frontend: C# WPF (.NET Framework 4.7.2 or .NET 6.0+)
•	Backend: Python 3.8+
•	Libraries: PyMuPDF, python-docx, openpyxl, PyInstaller
•	UI Theme: Pigeon Blue (#4A90E2) with Light Grey (#E8E8E8)

Software: 
•	Python 3.8+ 
•	Command Prompt (Administrator) 
•	Visual Studio 2026
•	Advanced Installer extension for Visual Studio 2026 by Caphyon 
•	GitHub account (optional)

Disclaimer: The code presented here is intended for an Install app. However, Windows security will probably block its execution. I have created the application file with install option using the Advanced Installer extension for Visual Studio 2026 by Caphyon.

The downloadable ZIP file is available at https://zenodo.org/records/18217095 (DOI: 10.5281/zenodo.18217095). Unzip the folder and activate “HTD Analyzer.exe”.
I have also attached a simple PDF file, “Test_document.pdf”, which identifies white text and 0,9 font.   
