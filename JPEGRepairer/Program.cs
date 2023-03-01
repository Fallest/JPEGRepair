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
    }

    /*
    Loops over every directory found in the given directory.
    If it finds another directory inside, it will call itself.
    */
    private static void RecoverDirectory(string directory, DirectoryInfo outDirectory) {
      // Skip output folders
      if (directory.Contains(OutFolderName)) return;
      Console.WriteLine($"{new String('-', directory.Split('\\').Length * 2)} \"{directory}\"");

      int jpgCounter = 0;
      int aviCounter = 0;
      DirectoryInfo currentFolderOutDirectory = Directory.CreateDirectory(outDirectory.FullName + directory);

      // Loop over JPG files
      foreach (string file in Directory.GetFiles(directory, "*.jpg"))
      {
        SaveFile($"{currentFolderOutDirectory.FullName}\\{file.Split('\\').Last()}", SkipBytes(file));
        jpgCounter++;
      }

      // Loop over AVI files
      foreach (string file in Directory.GetFiles(directory, "*.avi"))
      {
        SaveFile($"{currentFolderOutDirectory.FullName}\\{file.Split('\\').Last()}", SkipBytes(file));
        aviCounter++;
      }

      // User feedback
      Console.WriteLine("\n");
      Console.WriteLine($"{jpgCounter} JPG files processed inside \"{directory}\"");
      Console.WriteLine($"{aviCounter} AVI files processed inside \"{directory}\"");
      Console.WriteLine("\n");
      
      // Look for subdirectories
      foreach (string subDir in Directory.GetDirectories(directory))
      {
        RecoverDirectory(subDir, outDirectory);
      }
    }

    /*
    Returns a byte array after skipping the indicated bytes
    */
    private static byte[]? SkipBytes(string file)
    {
      try
      {
        // Setup reader stream at start of file, and skip the given bytes.
        bool isHealthy = false;
        FileStream reader = new FileStream(file, FileMode.Open);
        reader.Seek(0, SeekOrigin.Begin);

        /*
        If the first two bytes are 0xFF 0xD8, leave this file be.
        */
        int firstByte = reader.ReadByte();
        int secondByte = reader.ReadByte();
        if (
          (file.EndsWith("jpg") && firstByte == 0xFF && secondByte == 0xD8) || 
          (file.EndsWith("avi") && firstByte == 0x52 && secondByte == 0x49)
        ) {
          Console.WriteLine($"\"{file}\" is healthy.");
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