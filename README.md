# QFX to QIF (2013)

License: MIT (https://opensource.org/license/mit-0)

Convert QFX file format to QIF file format with Account Name header 

In a pinch, I needed access to some older Quicken data for one task only.   I was able to download the data I needed in QFX format, 
but I didn't want to spend the $100 for the Quicken license.  I found the Quicken 2013 was offered free of charge without the connected services.
The QFX file format is, reasonably, considered a connected service.   I found a free QFX to QIF converter online, but Quicken 2013 would not
import it.   Turns out it didn't import it because it didn't have the matching account name information.   After figuring out the needed
header information and manually adding it, I had a solution.  I thought!   The online converter (and every other converter I found) limited the
number of transaction unless you paid.   Again, didn't want to drop the money for a one time use.   And I'm a programmer.   So here is QFX to QIF.

---

## 🚀 Features

* **Convert QFX to QIF** — Converts all QFX records to QIF format without limitations.
* **Add Account Header** — Given an account name, adds the needed header information needed for Quicken 2013.

## 🛠️ Tech Stack

* **Windows Form** 
* **.NET 9.0**
* **Visual Studio 2022**

---

## 📦 Installation

Follow these steps to set up the project locally.

## Setup Steps

1. **Clone the repository:**
   ```bash
   git clone https://github.com/andersontechnologygroup/QFXtoQIF2013.git
   cd your-repo-name
   ```

2. **Start the local development environment:**
   ```bash
   devenv.exe
   ```

---

## 💡 Usage

1. After running the program, click the Browse button.

This will open the Open Dialog.  Navigate to the QFX file that you want to convert.  Click Open.
The full file path and name will be populated to the File Name field.   You may also type or paste a full path and file name into the File Name field.

2. Enter the Account Name into the Account Name field.

This should be the exact name used in Quicken 2013.

3. Click the Convert button.

The program will open the QFX file, parse it and read all of the information in it.  As the file is processed, it will display the progress.  
When the conversion is finished, it will automatically open the Save File dialog.

4. Enter the file name and click Save

---

## 🤝 Contributing

We welcome community contributions! Please read our guidelines to get started:

1. Fork the Project repository.
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`).
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`).
4. Push to the Branch (`git push origin feature/AmazingFeature`).
5. Open a Pull Request for review.

---

## 📄 License

Distributed under the MIT License. 

---

## 📬 Contact

* **Project Lead:** Jason Anderson
* **Email:** jason@anderson-technology-group.com
* **Project Link:** [https://github.com/andersontechnologygroup/QFXtoQIF2013](https://github.com/andersontechnologygroup/QFXtoQIF2013)
