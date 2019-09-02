using System;

namespace shortenfilename
{
    class Program
    {
        private static string  Informationshortenfilename = "ShortenFileName:\r\n" + "Version: {0} \r\n" + "Syntax:\r\n" +
        "ShortenFileName --explore --folder <FolderPath> [--max <MaxLength>] \r\n" +
        "ShortenFileName --update --folder <FolderPath> [--max <MaxLength>] \r\n" +
        "ShortenFileName --help \r\n";

        static bool ParseCommandLine(string[] args,
            out string Action,
            out string Folder,
            out uint MaxLength)
        {
            string Version = "1.0.0.0";
            bool result = false;
            string ErrorMessage = string.Empty;
            Action = string.Empty;
            Folder = string.Empty;
            MaxLength = 127;

            if((args == null)||(args.Length == 0))
            {
                ErrorMessage = "No parameter in the command line";

            }
            else
            {
                int i = 0;
                while ((i < args.Length)&&(string.IsNullOrEmpty(ErrorMessage)))
                {
                    
                        switch(args[i++])
                        {
                            
                            case "--help":
                                Action = "help";
                                break;
                            case "--explore":
                                Action = "explore";
                                break;
                            case "--update":
                                Action = "update";
                                break;
                                
                            case "--folder":
                                if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                    Folder = args[i++];
                                else
                                    ErrorMessage = "Folder not set";
                                break;

                            case "--max":
                                if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                {
                                    if(!uint.TryParse( args[i++],out MaxLength))
                                        ErrorMessage = "Max Filename length wrong parameter";

                                }
                                else
                                    ErrorMessage = "Max Filename length not set";
                                break;


                            default:

                                if ((args[i - 1].ToLower() == "dotnet") ||
                                    (args[i - 1].ToLower() == "shortenfilename.dll") ||
                                    (args[i - 1].ToLower() == "shortenfilename.exe"))
                                    break;

                                ErrorMessage = "wrong parameter: " + args[i-1];
                                break;
                        }

                }
            }

            if(!string.IsNullOrEmpty(ErrorMessage))
            {
                Console.WriteLine(ErrorMessage);
                Console.WriteLine(string.Format(Informationshortenfilename,Version));
            }
            if(Action.Equals("help"))
                Console.WriteLine(string.Format(Informationshortenfilename,Version));

            if( ((Action.Equals("explore"))||(Action.Equals("update")) )&&
                (!string.IsNullOrEmpty(Folder)))                
                result = true;

            return result;
        }  
        static void Explore(string Folder, uint MaxLen)
        {
            string[] files = System.IO.Directory.GetFiles(Folder);
            if(files!=null)            
            {
                foreach(string file in files)
                {
                    string name = System.IO.Path.GetFileName(file);
                    if(name.Length>MaxLen)
                        Console.WriteLine("File: " + file );
                }                
            }
            string[] dirs = System.IO.Directory.GetDirectories(Folder);
            if(dirs!=null)            
            {
                foreach(string dir in dirs)
                {
                    Explore( dir,MaxLen);
                }                
            }
        }  
        static string GetNewName(string file, uint MaxLen, ref int suffix)
        {
            string result = string.Empty;
            string name = System.IO.Path.GetFileName(file);
            string ext = System.IO.Path.GetExtension(file);
            string dir = System.IO.Path.GetDirectoryName(file);
            string newName = name.Substring(0, (int)MaxLen - 5 - ext.Length);
            string newFile = System.IO.Path.Combine(dir,newName + "~" + suffix.ToString() + ext);
            while(System.IO.File.Exists(newFile))
            {
                suffix++;
                newFile = System.IO.Path.Combine(dir,newName + "~" + suffix.ToString() + ext);
            }
            result = newFile;

            
            return result;
        }
        static void Update(string Folder, uint MaxLen)
        {
            string[] files = System.IO.Directory.GetFiles(Folder);
            int suffix = 1;
            if(files!=null)            
            {
                foreach(string file in files)
                {
                    string name = System.IO.Path.GetFileName(file);
                    if(name.Length>MaxLen)
                    {
                        string Newfile = GetNewName(file,MaxLen,ref suffix);
                        if(!string.IsNullOrEmpty(Newfile))
                        {
                            System.IO.File.Move(file,Newfile);
                            Console.WriteLine("File:     " + file );
                            Console.WriteLine("New File: " + Newfile );
                            suffix++;
                        }
                    }
                }                
            }
            string[] dirs = System.IO.Directory.GetDirectories(Folder);
            if(dirs!=null)            
            {
                foreach(string dir in dirs)
                {
                    Update( dir,MaxLen);
                }                
            }
        }      
        static void Main(string[] args)
        {
            string Folder = string.Empty;
            string Action = string.Empty;
            uint MaxLen = 127;


            if(ParseCommandLine(args,out Action,  out Folder, out MaxLen))
            {
                if(Action.Equals("explore"))
                {
                    if(System.IO.Directory.Exists(Folder))
                    {
                        Console.WriteLine("Exploring " + Folder);
                        Explore(Folder,MaxLen);                        
                    }

                }
                else if(Action.Equals("update"))
                {
                    if(System.IO.Directory.Exists(Folder))
                    {
                        Console.WriteLine("Exploring " + Folder);
                        Update(Folder,MaxLen);                        
                    }

                }
            }
        }
    }
}
