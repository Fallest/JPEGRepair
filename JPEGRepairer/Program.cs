using System;
using System.IO;
using System.Linq;

namespace JPEGRepair
{
  internal class ByteShifter
  {
    private static string OutFolderName = "recovered";
    /*
    Asks for a root directory and iterates over every JPG and AVI file inside
    each directory.
    Will remove the first two bytes from each file if found corrupt.
    */
    static void Main(string[] args)
    {
      ExplainProgram();

      string rootDir = GetDirectory();
      DirectoryInfo outDirectory = Directory.CreateDirectory($"{rootDir}\\{OutFolderName}");

      RecoverDirectory(rootDir, outDirectory);

      Console.WriteLine("\n\nPress any key to exit the program");
      Console.ReadKey();
    }

    /*
    Loops over every directory found in the given directory.
    If it finds another directory inside, it will call itself.
    */
    private static void RecoverDirectory(string directory, DirectoryInfo outDirectory) {
      // Skip output folders
      if (directory.Contains(OutFolderName)) return;
      Console.WriteLine($"{new String('-', directory.Split('\\').Length * 2)} \"{directory}\"");

      int jpgCounter = 0, aviCounter = 0, mp4Counter = 0;
      string lastFileName = "";
      DirectoryInfo currentFolderOutDirectory = Directory.CreateDirectory(outDirectory.FullName + directory);

      // Loop over JPG files
      foreach (string file in Directory.GetFiles(directory, "*.jpg"))
      {
        Console.Write($"\r{jpgCounter} JPG files processed inside {directory} (current file: {file})   ");
        SaveFile($"{currentFolderOutDirectory.FullName}\\{file.Split('\\').Last()}", SkipBytes(file));
        jpgCounter++;
        lastFileName = $"(current file: {file})";
      }
      Console.Write($"\r{jpgCounter} JPG files processed inside {directory} ---- DONE{new String(' ', lastFileName.Length)}\n");
      // Loop over AVI files
      foreach (string file in Directory.GetFiles(directory, "*.avi"))
      {
        Console.Write($"\r{aviCounter} AVI files processed inside {directory} (current file: {file})   ");
        SaveFile($"{currentFolderOutDirectory.FullName}\\{file.Split('\\').Last()}", SkipBytes(file));
        aviCounter++;
        lastFileName = $"(current file: {file})";
      }
      Console.Write($"\r{aviCounter} AVI files processed inside {directory} ---- DONE{new String(' ', lastFileName.Length)}\n");
      // Loop over MP4 files
      foreach (string file in Directory.GetFiles(directory, "*.mp4"))
      {
        Console.Write($"\r{mp4Counter} MP4 files processed inside {directory} (current file: {file})   ");
        SaveFile($"{currentFolderOutDirectory.FullName}\\{file.Split('\\').Last()}", SkipBytes(file));
        mp4Counter++;
        lastFileName = $"(current file: {file})";
      }
      Console.Write($"\r{mp4Counter} MP4files processed inside {directory} ---- DONE{new String(' ', lastFileName.Length)}\n");

      // Look for subdirectories
      foreach (string subDir in Directory.GetDirectories(directory))
      {
        RecoverDirectory(subDir, outDirectory);
      }
    }

    private static bool CheckFileIntegrity(string file, FileStream reader)
    {
      switch(file.Split('.').Last())
      {
        case "jpg":
        case "jpeg":
          {
            return CheckJPEGFileIntegrity(reader);
          }
        case "avi":
          {
            return CheckAVIFileIntegrity(reader);
          }
        case "mp4":
          {
            return CheckMP4FileIntegrity(reader);
          }
        default:
          {
            return false;
          }
      }
    }

    /*
    Returns true if the first two bytes are healthy (start of JPEG file). 
    */
    private static bool CheckJPEGFileIntegrity(FileStream reader) {
      reader.Seek(0, SeekOrigin.Begin);
      int firstByte = reader.ReadByte(), secondByte = reader.ReadByte();
      return firstByte == 0xFF && secondByte == 0xD8;
    }

    /*
    Returns true if the first two bytes are healthy (start of AVI file). 
    */
    private static bool CheckAVIFileIntegrity(FileStream reader) {
      reader.Seek(0, SeekOrigin.Begin);
      int firstByte = reader.ReadByte(), secondByte = reader.ReadByte();
      return firstByte == 0x52 && secondByte == 0x49;
    }

    /*
    Returns true if the first atom contained inside the MP4 is "ftyp". 
    */
    private static bool CheckMP4FileIntegrity(FileStream reader) {
      reader.Seek(4, SeekOrigin.Begin);
      byte[] ftypAtom = new byte[4];
      reader.Read(ftypAtom, 0, ftypAtom.Length);
      return System.Text.Encoding.Default.GetString(ftypAtom) == "ftyp" ;
    }

    /*
    Returns returns the contents of the file after checking its integrity and removing the first 2 bytes
    if it's not healthy.
    */
    private static byte[]? SkipBytes(string file)
    {
      try
      {
        bool isHealthy = false;
        FileStream reader = new FileStream(file, FileMode.Open);

        if (CheckFileIntegrity(file, reader)) {
          isHealthy = true;
        }

        // Store the contents
        reader.Seek(isHealthy ? 0 : 2, SeekOrigin.Begin);
        byte[] buffer = new byte[reader.Length];
        reader.Read(buffer, 0, buffer.Length);
        return buffer;
      }
      catch (FileNotFoundException)
      {
        Console.WriteLine($"Error at file \"{file}\": File not found.");
      }
      return null;
    }

    /*
    Saves the contents of the buffer inside a new file. 
    */
    private static void SaveFile(string savePath, byte[]? buffer)
    {
      if (buffer == null) return;
      using FileStream writer = new FileStream(savePath, FileMode.Create);
      writer.Write(buffer, 0, buffer.Length);
    }

    private static string GetDirectory()
    {
      string? directory = "";

      try
      {
        Console.Write("Directory (\"quit\" to exit the program): ");
        directory = Console.ReadLine();

        if (directory == "quit")
        {
          Console.WriteLine("Exiting...");
          System.Environment.Exit(0);
        }

        if (directory == null) throw new Exception();
      }
      catch (IOException)
      {
        Console.WriteLine("Invalid characters introduced. \nExiting...");
        System.Environment.Exit(-1);
      }
      catch (Exception)
      {
        Console.WriteLine("Error. Not a valid directory. \nExiting...");
        System.Environment.Exit(-1);
      }

      return directory;
    }
    
    private static void ExplainProgram()
    {
      // Program explanation
      Console.WriteLine("This program is used to recover JPG and AVI files that have their first two bytes corrupted.");
      Console.WriteLine("This program does not analyze other filetypes, and leaves healthy files as-is.");
      Console.WriteLine("This program is recursive, so it will analyze any and all subdirectories found inside the root directory.\n");
    }
  }
}