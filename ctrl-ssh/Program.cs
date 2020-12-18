using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace ctrl_ssh
{
    class Program
    {

        /*
         * A SSH MANAGER FOR WINDOWS AND MAC OS X
         * USE UP TO 20 ENTRIES
         * SUPPORTS PASSWORD AND KEY AUTH
         * YOU STILL HAVE TO ENTER THE PASSWORD 
         * IF THERE IS NO KEY FOR THE ENTRY
         * -----------------------------
         * USE THE 3 LINER HELP AT THE BOTTOM
         * FOR INSTRUCTIONS
         */

        //Keep track of the current colors, used to change colors
        static ConsoleColor bgColor;
        static ConsoleColor fgColor;
        //Default selected entry
        static int selectedEntry = 1;
        //Add, Edit operations
        public const int ADD = 0;
        public const int EDIT = 1;
        //Global Y Coordinate for the screen
        //If any changes in the design, apply the coord here
        static int ycoord = 3;
        //Terminal location
        public static string termloc;
        //Config file folder/directory
        //C:\Users\<username>\AppData\Local under Windows
        //Users/<username>/.local/share
        public static string cfgFileName = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        //Delete key
        public static char deleteKey;
        //Handler to save the config file
        public static StreamWriter sw;
        //Shared secred to encrypt/decrypt the config file
        public static string sharedSecret = 
            @"EwQCMAAwPgYDVR0fBDcwNTAzoDGgL4YtaHR0cDovL2NybDIuYWxwaGFzc2wuY29t
            L2dzL2dzYWxwaGFzaGEyZzIuY3JsMCkGA1UdEQQiMCCCDyoubmFub2Nsb3VkLm9y
            Z4INbmFub2Nsb3VkLm9yZzAdBgNVHSUEFjAUBggrBgEFBQcDAQYIKwYBBQUHAwIw
            HwYDVR0jBBgwFoAU9c3VPAhQ+WpPOreX2laD5mnSaPcwHQYDVR0OBBYEFNUKwNyI
            mlTZL/EcuP0onEsE6Ck9MIIBBQYKKwYBBAHWeQIEAgSB9gSB8wDxAHcApLkJkLQY";
        //Definition of the entries
        public struct Entry
        {
            public string hostname;
            public string username;
            public string password;
            public string keyfile;
            public string descript;
        }
        //Array of 20 entries
        public static Entry[] allEntries = new Entry[20];
        //Definition of screen coordinates
        public struct Coords
        {
            public int X;
            public int Y;
        }
        //Return values for the input/editor routine for the params
        public struct ParamInput
        {
            public string text;
            public char key;
        }

        //Execute when the program is terminated with Q or the window closed with mouse
        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {            
            Console.CursorVisible = true;
            WriteConfigFile();//Delete the file on OSX
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (File.Exists("/tmp/ctrl-ssh.sh"))
                {
                    File.Delete("/tmp/ctrl-ssh.sh");
                }
            }
        }        
        
        //Draws a text window with set coordinates, size and type
        public static void TextWindow(int intX, int intY, int intWidth, int intLength, int intType)
        {

            char cTopLeft = ' ', cTopRight = ' ', cBottomLeft = ' ',
                cBottomRight = ' ', cHorizontal = ' ', cVertical = ' ';
            if (intType == 0)
            {
                cTopLeft = '╔';
                cTopRight = '╗';
                cBottomLeft = '╚';
                cBottomRight = '╝';
                cHorizontal = '═';
                cVertical = '║';
            }
            if (intType == 1)
            {
                cTopLeft = '┌';
                cTopRight = '┐';
                cBottomLeft = '└';
                cBottomRight = '┘';
                cHorizontal = '─';
                cVertical = '│';
            }

            //Top
            Console.SetCursorPosition(intX, intY);
            Console.Write(cTopLeft);
            for (int i = 0; i < intWidth; i++) Console.Write(cHorizontal);
            Console.SetCursorPosition(intX + intWidth, intY);
            Console.Write(cTopRight);
            //Bottom
            Console.SetCursorPosition(intX, intY + intLength);
            Console.Write(cBottomLeft);
            for (int i = 0; i < intWidth; i++) Console.Write(cHorizontal);
            Console.SetCursorPosition(intX + intWidth, intY + intLength);
            Console.Write(cBottomRight);
            //Left            
            for (int i = 1; i < intLength; i++)
            {
                Console.SetCursorPosition(intX, intY + i);
                Console.Write(cVertical);
            }
            //Right
            for (int i = 1; i < intLength; i++)
            {
                Console.SetCursorPosition(intX + intWidth, intY + i);
                Console.Write(cVertical);
            }
        }
        
        //Sets text colors, if the color is 0, don't change that color
        public static void SetCurrentColors(ConsoleColor bgc, ConsoleColor fgc)
        {
            if (bgc != 0) Console.BackgroundColor = bgc;
            if (fgc != 0) Console.ForegroundColor = fgc;
        }
        
        //Remeber the current colors in global variables
        public static void PushCurrentColors()
        {            
            bgColor = Console.BackgroundColor;
            fgColor = Console.ForegroundColor;
        }
        
        //Restore the current colors
        public static void PopCurrentColors()
        {
            Console.BackgroundColor = bgColor;
            Console.ForegroundColor = fgColor;
        }
        
        //Returns screen coordinates for an entry
        public static Coords FindEntryCoords(int i)
        {
            int x, y;

            if (i >= 1 && i <= 10)
            {
                x = 3;
                y = ycoord + 7 + i;
            }
            else
            {
                x = 40;
                y = ycoord + 7 + i - 10;
            }
            Coords outc = new Coords
            {
                X = x,
                Y = y
            };
            return outc;
        }        
        
        //Resets an old entry an selects a new one
        public static void SelectEntry(int i)
        {
            //Reset old selected entry
            Coords screen = new Coords();
            screen = FindEntryCoords(selectedEntry);
            Console.SetCursorPosition(screen.X, screen.Y);
            Console.Write("  ");
            //Set the new selected entry
            screen = FindEntryCoords(i);
            Console.SetCursorPosition(screen.X, screen.Y);
            PushCurrentColors();
            SetCurrentColors(ConsoleColor.Green, ConsoleColor.Black);            
            Console.Write(">>");
            PopCurrentColors();
            selectedEntry = i;
            //Redraw the underlines under params
            string strUnderlineParams = new string('_', 61);
            for (int x = 0; x < 5; x++)
            {
                Console.SetCursorPosition(15, 1 + x);
                Console.WriteLine(strUnderlineParams);
            }
            PrintParams(i);
        }
        
        //Print the parameters for the selected entry
        public static void PrintParams(int entry)
        {
            string strUnderlineParams = new string('_', 61);
            int pwlen;
            for (int x = 0; x < 5; x++)
            {
                Console.SetCursorPosition(15, 1 + x);
                Console.WriteLine(strUnderlineParams);
            }

            Console.SetCursorPosition(15, 1);
            Console.WriteLine(allEntries[entry - 1].hostname);
            Console.SetCursorPosition(15, 2);
            Console.WriteLine(allEntries[entry - 1].username);
            Console.SetCursorPosition(15, 3);
            if (string.IsNullOrEmpty(allEntries[entry - 1].password)) pwlen = 0;
              else pwlen = allEntries[entry - 1].password.Length;
            Console.WriteLine(new string('*', pwlen));
            Console.SetCursorPosition(15, 4);
            Console.WriteLine(allEntries[entry - 1].keyfile);
            Console.SetCursorPosition(15, 5);
            Console.WriteLine(allEntries[entry - 1].descript);
        }
        
        //Prints the main screen
        public static void PrintMainScreen()
        {
            string strUnderlineParams = new string('_', 61);
            string strUnderlineEntries = new string('_', 30);
            Console.CursorVisible = false;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            { 
                Console.SetWindowSize(79, 25);
                Console.BufferWidth = Console.WindowWidth = 80;
                Console.BufferHeight = Console.WindowHeight;
            }
            SetCurrentColors(ConsoleColor.DarkBlue, ConsoleColor.DarkBlue);
            Console.CursorVisible = false;
            Console.Clear();
            SetCurrentColors(ConsoleColor.DarkBlue, ConsoleColor.Yellow);
            //Print PARAMS
            ycoord = 0;
            TextWindow(0, ycoord, 79, 6, 0);
            Console.SetCursorPosition(3, ycoord);
            PushCurrentColors();
            SetCurrentColors(ConsoleColor.White, ConsoleColor.DarkBlue);
            Console.Write("[PARAMS]");
            PopCurrentColors();
            Console.SetCursorPosition(5, ycoord + 1);
            Console.Write("HOST||IP: " + strUnderlineParams);
            Console.SetCursorPosition(5, ycoord + 2);
            Console.Write("USERNAME: " + strUnderlineParams);
            Console.SetCursorPosition(5, ycoord + 3);
            Console.Write("PASSWORD: " + strUnderlineParams);
            Console.SetCursorPosition(5, ycoord + 4);
            Console.Write("KEY FILE: " + strUnderlineParams);
            Console.SetCursorPosition(5, ycoord + 5);
            Console.Write("DESCRIPT: " + strUnderlineParams);
            //Print ENTRIES            
            TextWindow(0, ycoord + 7, 79, 11, 0);
            Console.SetCursorPosition(3, ycoord + 7);
            PushCurrentColors();
            SetCurrentColors(ConsoleColor.White, ConsoleColor.DarkBlue);
            Console.Write("[ENTRIES]");
            PopCurrentColors();
            for (int i = 0; i < 10; i++)
            {
                Console.SetCursorPosition(5, ycoord + 8 + i);
                string entriesNo = (i + 1).ToString();
                if (entriesNo == "10") entriesNo = "0";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
                    Console.Write(entriesNo + ". " + strUnderlineEntries +
                    "    " + '^' + entriesNo + ". " + strUnderlineEntries);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    Console.Write(entriesNo + ". " + strUnderlineEntries +
                    "    " + '^' + (char)(i + 79) + ". " + strUnderlineEntries);
            }
            //Print MENU            
            TextWindow(0, ycoord + 19, 79, 2, 0);
            Console.SetCursorPosition(3, ycoord + 19);
            PushCurrentColors();
            SetCurrentColors(ConsoleColor.White, ConsoleColor.DarkBlue);
            Console.Write("[MENU]");
            PopCurrentColors();
            Console.SetCursorPosition(6, ycoord + 20);
            Console.Write("[A]DD            [E]DIT               [D]ELETE                [Q]UIT");
            //Logo
            Console.SetCursorPosition(15, 0);
            PushCurrentColors();
            SetCurrentColors(ConsoleColor.Red, 0);
            Console.Write("CTRL-SSH v0.0.1 - K.Andreev (https://blog.andreev.it)");
            PopCurrentColors();
            //Help
            PushCurrentColors();
            SetCurrentColors(ConsoleColor.Magenta, ConsoleColor.White);
            Console.SetCursorPosition(1, 22);            
            Console.Write("    [PARAMS] - USE ENTER TO MOVE NEXT, ESC TO CANCEL                          ");
            Console.SetCursorPosition(1, 23);
            Console.Write("   [ENTRIES] - USE THE # OR CTRL-# TO SELECT, ENTER TO LAUNCH SSH OR [MENU]   ");
            Console.SetCursorPosition(1, 24);
            Console.Write("      [MENU] - USE A, E, D OR Q TO SELECT FROM THE MENU AND MANAGE AN ENTRY   ");
            PopCurrentColors();
        }
        
        //Write config file under local app folder
        public static void WriteConfigFile()
        {
            if (sw == null) sw = File.CreateText(cfgFileName);
            for (int x = 0; x < 20; x++)
            {
                if (string.IsNullOrEmpty(allEntries[x].hostname)) allEntries[x].hostname = " ";
                if (string.IsNullOrEmpty(allEntries[x].username)) allEntries[x].username = " ";
                if (string.IsNullOrEmpty(allEntries[x].password)) allEntries[x].password = " ";
                if (string.IsNullOrEmpty(allEntries[x].keyfile)) allEntries[x].keyfile = " ";
                if (string.IsNullOrEmpty(allEntries[x].descript)) allEntries[x].descript = " ";
                sw.WriteLine((x + 1).ToString());
                sw.WriteLine(EncryptString(allEntries[x].hostname, sharedSecret));
                sw.WriteLine(EncryptString(allEntries[x].username, sharedSecret));
                sw.WriteLine(EncryptString(allEntries[x].password, sharedSecret));
                sw.WriteLine(EncryptString(allEntries[x].keyfile, sharedSecret));
                sw.WriteLine(EncryptString(allEntries[x].descript, sharedSecret));
            }
            sw.Close(); 
        }
        
        //Read config file from local app folder
        public static void ReadConfigFile()
        {                       
            try
            {
                string[] allLines = File.ReadAllLines(cfgFileName);
                for (int x = 0; x < 20; x++)
                {
                    allEntries[x].hostname = DecryptString(allLines[6 * x + 1], sharedSecret).Trim();
                    allEntries[x].username = DecryptString(allLines[6 * x + 2], sharedSecret).Trim();
                    allEntries[x].password = DecryptString(allLines[6 * x + 3], sharedSecret).Trim();
                    allEntries[x].keyfile = DecryptString(allLines[6 * x + 4], sharedSecret).Trim();
                    allEntries[x].descript = DecryptString(allLines[6 * x + 5], sharedSecret).Trim();
                }
            }
            
            catch (FileNotFoundException)
            {
                sw = File.CreateText(cfgFileName);
            }
            PrintAllEntries();
        }
        
        //Prints all entries from the allEntries array to ENTRY screen section
        public static void PrintAllEntries()
        {
            int hostlen;
            for (int x = 0; x < 10; x++)
            {
                Console.SetCursorPosition(8, 8 + x);
                if (string.IsNullOrEmpty(allEntries[x].hostname)) hostlen = 0;
                else hostlen = allEntries[x].hostname.Length;
                if (hostlen > 30) Console.WriteLine(allEntries[x].hostname.Substring(0, 30)); 
                    else Console.WriteLine(allEntries[x].hostname);

            }
            for (int x = 10; x < 20; x++)
            {
                Console.SetCursorPosition(46, x - 2);
                if (string.IsNullOrEmpty(allEntries[x].hostname)) hostlen = 0;
                else hostlen = allEntries[x].hostname.Length;
                if (hostlen > 30) Console.WriteLine(allEntries[x].hostname.Substring(0, 30));
                    else Console.WriteLine(allEntries[x].hostname);
            }
        }

        //Encrypt
        public static string EncryptString(string text, string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key must have valid value.", nameof(key));
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("The text must have valid value.", nameof(text));

            var buffer = Encoding.UTF8.GetBytes(text);
            var hash = new SHA512CryptoServiceProvider();
            var aesKey = new byte[24];
            Buffer.BlockCopy(hash.ComputeHash(Encoding.UTF8.GetBytes(key)), 0, aesKey, 0, 24);

            using (var aes = Aes.Create())
            {
                if (aes == null)
                    throw new ArgumentException("Parameter must not be null.", nameof(aes));

                aes.Key = aesKey;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var resultStream = new MemoryStream())
                {
                    using (var aesStream = new CryptoStream(resultStream, encryptor, CryptoStreamMode.Write))
                    using (var plainStream = new MemoryStream(buffer))
                    {
                        plainStream.CopyTo(aesStream);
                    }

                    var result = resultStream.ToArray();
                    var combined = new byte[aes.IV.Length + result.Length];
                    Array.ConstrainedCopy(aes.IV, 0, combined, 0, aes.IV.Length);
                    Array.ConstrainedCopy(result, 0, combined, aes.IV.Length, result.Length);

                    return Convert.ToBase64String(combined);
                }
            }
        }

        //Decrypt
        public static string DecryptString(string encryptedText, string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key must have valid value.", nameof(key));
            if (string.IsNullOrEmpty(encryptedText))
                throw new ArgumentException("The encrypted text must have valid value.", nameof(encryptedText));

            var combined = Convert.FromBase64String(encryptedText);
            var buffer = new byte[combined.Length];
            var hash = new SHA512CryptoServiceProvider();
            var aesKey = new byte[24];
            Buffer.BlockCopy(hash.ComputeHash(Encoding.UTF8.GetBytes(key)), 0, aesKey, 0, 24);

            using (var aes = Aes.Create())
            {
                if (aes == null)
                    throw new ArgumentException("Parameter must not be null.", nameof(aes));

                aes.Key = aesKey;

                var iv = new byte[aes.IV.Length];
                var ciphertext = new byte[buffer.Length - iv.Length];

                Array.ConstrainedCopy(combined, 0, iv, 0, iv.Length);
                Array.ConstrainedCopy(combined, iv.Length, ciphertext, 0, ciphertext.Length);

                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var resultStream = new MemoryStream())
                {
                    using (var aesStream = new CryptoStream(resultStream, decryptor, CryptoStreamMode.Write))
                    using (var plainStream = new MemoryStream(ciphertext))
                    {
                        plainStream.CopyTo(aesStream);                                                
                    }

                    return Encoding.UTF8.GetString(resultStream.ToArray());
                }
            }
        }

        //Function for restricted keyboard input
        //Limit entry to "limit" chars
        //Used for params entry
        public static ParamInput ReadLineLimited(int limit, string text)
        {
            char typedKey;
            string tmpEntry = text;
            while (true)
            {
                readLabel:
                typedKey = Console.ReadKey(true).KeyChar;
                //enter
                if (typedKey == '\r') 
                    break;
                //escape
                if (typedKey == '\u001b')
                    break;
                //Cursors
                if (typedKey == '\0')
                    goto readLabel;
                //Tab
                if (typedKey == '\t')
                    goto readLabel;
                //Delete key value differs on OSX and Windows
                if (typedKey == deleteKey)
                {
                    if (tmpEntry != "")
                    {
                        tmpEntry = tmpEntry.Substring(0, tmpEntry.Length - 1);
                        Console.Write("\b_\b");
                    }
                }
                else if (tmpEntry.Length < limit)
                {
                    Console.Write(typedKey);
                    tmpEntry += typedKey;
                }
            }

            ParamInput pi = new ParamInput();
            pi.text = tmpEntry;
            pi.key = typedKey;
            return pi;
        }

        //Prints the hostname in the entries
        public static void PrintHostname(int entry)
        {
            string strUnderlineEntries = new string('_', 30);
            int xc, yc;
            if (entry < 11) 
            {
                xc = 8;
                yc = 7;
            } 
            else 
            {
                xc = 46;
                yc = - 3;
            }
            Console.SetCursorPosition(xc, yc + entry);
            Console.WriteLine(strUnderlineEntries);
            Console.SetCursorPosition(xc, yc + entry);
            Console.WriteLine(allEntries[entry - 1].hostname);
        }

        //Add entry
        public static void AddEntry(int entry)
        {
            if (!string.IsNullOrEmpty(allEntries[entry - 1].hostname)) return;
            MiniEditor(entry, ADD);
            Console.CursorVisible = false;
            PrintHostname(entry);
        }

        //Edit entry
        public static void EditEntry(int entry)
        {
            if (string.IsNullOrEmpty(allEntries[entry - 1].hostname)) return;
            MiniEditor(entry, EDIT);
            Console.CursorVisible = false;
            PrintHostname(entry);
        }

        //Mini editor
        public static void MiniEditor(int entry, int ops)
        {
            int line = 0;
            do
            {
                ParamInput paramInput = new ParamInput();
                
                if (ops == ADD)
                {
                    Console.SetCursorPosition(15, 1 + line);
                    Console.CursorVisible = true;
                    paramInput = ReadLineLimited(60, "");                                        
                }
                else if (ops == EDIT)
                {
                    Console.SetCursorPosition(15 + allEntries[entry -1].hostname.Length, 1 + line);
                    Console.CursorVisible = true;
                    string val = "";
                    switch (line)
                    {                        
                        case 0:
                            val = allEntries[entry - 1].hostname;
                            Console.SetCursorPosition(15 + allEntries[entry - 1].hostname.Length, 1 + line);
                            break;
                        case 1:
                            val = allEntries[entry - 1].username;
                            Console.SetCursorPosition(15 + allEntries[entry - 1].username.Length, 1 + line);
                            break;
                        case 2:
                            val = allEntries[entry - 1].password;
                            Console.SetCursorPosition(15 + allEntries[entry - 1].password.Length, 1 + line);
                            break;
                        case 3:
                            val = allEntries[entry - 1].keyfile;
                            Console.SetCursorPosition(15 + allEntries[entry - 1].keyfile.Length, 1 + line);
                            break;
                        case 4:
                            val = allEntries[entry - 1].descript;
                            Console.SetCursorPosition(15 + allEntries[entry - 1].descript.Length, 1 + line);
                            break;
                    }
                    paramInput = ReadLineLimited(60, val);
                }
                //If ESC go back
                if (paramInput.key == '\u001b')
                {
                    Console.CursorVisible = false;
                    return;
                }
                //Advance to next line if ENTER is pressed
                if (paramInput.key == '\r')
                {
                    line++;
                    switch (line)
                    {
                        case 1:
                            allEntries[entry - 1].hostname = paramInput.text;
                            break;
                        case 2:
                            allEntries[entry - 1].username = paramInput.text;
                            break;
                        case 3:
                            allEntries[entry - 1].password = paramInput.text;
                            break;
                        case 4:
                            allEntries[entry - 1].keyfile = paramInput.text;
                            break;
                        case 5:
                            allEntries[entry - 1].descript = paramInput.text;
                            break;
                    }
                }
            } while (line != 5);
        }

        //Delete entry
        public static void DeleteEntry(int entry)
        {
            string strUnderlineParams = new string('_', 61);
            for (int x = 0; x < 5; x++)
            {
                Console.SetCursorPosition(15, 1 + x);
                Console.WriteLine(strUnderlineParams);
            }
            allEntries[entry - 1].hostname = "";
            allEntries[entry - 1].username = "";
            allEntries[entry - 1].password = "";
            allEntries[entry - 1].keyfile = "";
            allEntries[entry - 1].descript = "";
            PrintHostname(entry);
        }

        //Open entry
        public static void OpenEntry(int entry)
        {
            //Return if no hostname
            if (string.IsNullOrEmpty(allEntries[entry - 1].hostname)) return;
            Process proc = new Process();

            //For Windows use these commands
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                proc.StartInfo.FileName = termloc;
                //If key file is blank or empty, use password
                if (string.IsNullOrEmpty(allEntries[entry - 1].keyfile))
                {
                    proc.StartInfo.Arguments = "start powershell 'ssh " + allEntries[entry - 1].username + "@" + allEntries[entry - 1].hostname + "'";
                }
                else
                {
                    proc.StartInfo.Arguments = "start powershell 'ssh " + allEntries[entry - 1].username + "@" + allEntries[entry - 1].hostname +
                        " -i ''" + "\"" + @allEntries[entry - 1].keyfile + "\"" + "'''";
                }
            }
            //For OSX use these commands
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                //Create the temp file /tmp/ctrlsshexec.sh
                if (string.IsNullOrEmpty(allEntries[entry - 1].keyfile))
                {
                    string cmd = "ssh " + allEntries[entry - 1].username + "@"
                        + allEntries[entry - 1].hostname;
                    File.WriteAllText("/tmp/ctrl-ssh.sh", cmd);
                }
                else
                {
                    string cmd = "ssh -i '" + allEntries[entry - 1].keyfile + "' "
                        + allEntries[entry - 1].username + "@" + allEntries[entry - 1].hostname;
                    File.WriteAllText("/tmp/ctrl-ssh.sh", cmd);
                }
                //Change permissions
                Exec("chmod +x /tmp/ctrl-ssh.sh");

                proc.StartInfo.FileName = termloc;
                proc.StartInfo.Arguments = "-a Terminal /tmp/ctrl-ssh.sh";
            }
            //Execute the command
            proc.Start();
        }

        //Function to change permissions on the temp file in OSX
        public static void Exec(string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "/bin/sh",
                    Arguments = $"-c \"{escapedArgs}\""
                }
            };

            process.Start();
            process.WaitForExit();
        }

        //Main program
        static void Main(string[] args)
         {
            //Define these variables for Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                termloc = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe";
                cfgFileName  = cfgFileName + "\\ctrl-ssh.cfg";
                deleteKey = '\b';
            }
            //Define these variables for OSX
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                termloc = "/usr/bin/open";
                cfgFileName = cfgFileName + "/ctrl-ssh.cfg";
                deleteKey = '\u007f';                ;
            }

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);                                 
            ConsoleKeyInfo cki;          
            PrintMainScreen();
            ReadConfigFile();
            SelectEntry(1);
            do
            {
                cki = Console.ReadKey(true);
                switch (cki.Key)
                {
                    //#1 and Ctrl-1
                    case ConsoleKey.D1:
                        if (cki.Modifiers == ConsoleModifiers.Control) SelectEntry(11);
                        else SelectEntry(1);
                        break;
                    case ConsoleKey.D2:
                        if (cki.Modifiers == ConsoleModifiers.Control) SelectEntry(12);
                        else SelectEntry(2);
                        break;
                    case ConsoleKey.D3:
                        if (cki.Modifiers == ConsoleModifiers.Control) SelectEntry(13);
                        else SelectEntry(3);
                        break;
                    case ConsoleKey.D4:
                        if (cki.Modifiers == ConsoleModifiers.Control) SelectEntry(14);
                        else SelectEntry(4);
                        break;
                    case ConsoleKey.D5:
                        if (cki.Modifiers == ConsoleModifiers.Control) SelectEntry(15);
                        else SelectEntry(5);
                        break;
                    case ConsoleKey.D6:
                        if (cki.Modifiers == ConsoleModifiers.Control) SelectEntry(16);
                        else SelectEntry(6);
                        break;
                    case ConsoleKey.D7:
                        if (cki.Modifiers == ConsoleModifiers.Control) SelectEntry(17);
                        else SelectEntry(7);
                        break;
                    case ConsoleKey.D8:
                        if (cki.Modifiers == ConsoleModifiers.Control) SelectEntry(18);
                        else SelectEntry(8);
                        break;
                    case ConsoleKey.D9:
                        if (cki.Modifiers == ConsoleModifiers.Control) SelectEntry(19);
                        else SelectEntry(9);
                        break;
                    case ConsoleKey.D0:
                        if (cki.Modifiers == ConsoleModifiers.Control) SelectEntry(20);
                        else SelectEntry(10);
                        break;
                    //OS X can't detect CTRL and # due to a different terminal keyboard mappings
                    case ConsoleKey.O:
                        if (cki.Modifiers == ConsoleModifiers.Control && 
                            RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) SelectEntry(11);
                        break;
                    case ConsoleKey.P:
                        if (cki.Modifiers == ConsoleModifiers.Control && 
                            RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) SelectEntry(12);
                        break;
                    case ConsoleKey.Q:
                        if (cki.Modifiers == ConsoleModifiers.Control &&
                            RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) SelectEntry(13);
                        break;
                    case ConsoleKey.R:
                        if (cki.Modifiers == ConsoleModifiers.Control && 
                            RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) SelectEntry(14);
                        break;
                    case ConsoleKey.S:
                        if (cki.Modifiers == ConsoleModifiers.Control &&
                            RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) SelectEntry(15);
                        break;
                    case ConsoleKey.T:
                        if (cki.Modifiers == ConsoleModifiers.Control && 
                            RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) SelectEntry(16);
                        break;
                    case ConsoleKey.U:
                        if (cki.Modifiers == ConsoleModifiers.Control && 
                            RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) SelectEntry(17);
                        break;
                    case ConsoleKey.V:
                        if (cki.Modifiers == ConsoleModifiers.Control && 
                            RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) SelectEntry(18);
                        break;
                    case ConsoleKey.W:
                        if (cki.Modifiers == ConsoleModifiers.Control && 
                            RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) SelectEntry(19);
                        break;
                    case ConsoleKey.X:
                        if (cki.Modifiers == ConsoleModifiers.Control && 
                            RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) SelectEntry(20);
                        break;
                    //Key 'A'
                    case ConsoleKey.A:
                        AddEntry(selectedEntry);
                        break;
                    case ConsoleKey.D:
                        DeleteEntry(selectedEntry);
                        break;
                    case ConsoleKey.E:
                        EditEntry(selectedEntry);
                        break;
                    case ConsoleKey.Enter:
                        OpenEntry(selectedEntry);
                        break;
                }
            }
            //Repeat while Q is not pressed
            while (cki.Key != ConsoleKey.Q);
        }
    }
}