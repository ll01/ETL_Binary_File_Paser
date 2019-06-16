using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Threading.Tasks;
using System.Text;
using System.IO.Compression;
using System.Diagnostics;

namespace openfile
{
    static class Program
    {
        class DataBaseSettings
        {
            const int white = 255;
            const int image_offset = 16;
            private int m_offsetinbytes;
            private int m_recordsizeinbytes;
            private int m_image_width;
            private int m_image_hight;
            private int m_metadataoffsetinbytes;
            private int m_lableoffset;
            private int m_packagesize;
            private int m_lablesizeinbytes;
            private Encoding m_labelencoding;
            private int m_colourLevels;
            private int m_colourdepthinbits;
            Task SaveTask = null;
            Task SetPixels = null;


            public int Offsetinbytes { get => m_offsetinbytes; set => m_offsetinbytes = value; }
            public int Recordsizeinbytes { get => m_recordsizeinbytes; set => m_recordsizeinbytes = value; }
            public int Image_width { get => m_image_width; set => m_image_width = value; }
            public int Image_hight { get => m_image_hight; set => m_image_hight = value; }
            public int Metadataoffsetinbytes { get => m_metadataoffsetinbytes; set => m_metadataoffsetinbytes = value; }
            public int Lableoffset { get => m_lableoffset; set => m_lableoffset = value; }
            public int Packagesize { get => m_packagesize; set => m_packagesize = value; }
            public int Lablesizeinbytes { get => m_lablesizeinbytes; set => m_lablesizeinbytes = value; }
            public int ColourLevels { get => m_colourLevels; set => m_colourLevels = value; }

            public DataBaseSettings(int offsetinbytes, int recordsizeinbytes, int image_width, int image_hight,
                                    int metadataoffset, int lableoffset, int lablesizeinbytes, Encoding labelencoding, int colourdepthinbits)
            {

                m_offsetinbytes = offsetinbytes;
                m_recordsizeinbytes = recordsizeinbytes;
                m_image_width = image_width;
                m_image_hight = image_hight;
                m_metadataoffsetinbytes = metadataoffset;
                m_lableoffset = lableoffset;
                m_packagesize = m_recordsizeinbytes - m_metadataoffsetinbytes;
                m_lablesizeinbytes = lablesizeinbytes;
                m_labelencoding = labelencoding;
                m_colourLevels = (int)(256 / Math.Pow(2, colourdepthinbits));
                m_colourdepthinbits = colourdepthinbits;
            }

            public string getLable(byte[] lableByteArray)
            {
                string Lable = null;
                switch (m_labelencoding.WebName.ToUpper())
                {
                    case "EUC-JP":
                        // convert  jis x 2080 to euc you need to add 127 to the value
                        Lable = m_labelencoding.GetString(lableByteArray.Select(x => (byte)(x + 128)).ToArray());
                        break;
                    default:
                        Lable = m_labelencoding.GetString(lableByteArray);
                        break;

                }
                return Lable;
            }

            public async Task ExtractDataset(string FolderPath, string saveFolderPath, bool tocrop)
            {


                //var FileStream = File.Open(@"D:\Datasets\ETL9G_06", FileMode.Open);
                //var FileStream = File.Open(@"D:\Downloads\ETL1\ETL1\ETL1C_01", FileMode.Open);
                byte[] bytedata = new byte[Recordsizeinbytes - m_metadataoffsetinbytes];


                List<string> lables = new List<string>();
                //read file into byte array offseting 33 (for the meta data)


                foreach (var currentFile in Directory.GetFiles(FolderPath).Where(x => Path.GetExtension(x) == ""))
                //try
                {

                    var FileStream = File.Open(currentFile, FileMode.Open);
                    //convert to bitarray since the colours are stored as  4 bit collour depth
                    for (int recordCount = 0; recordCount < FileStream.Length / Recordsizeinbytes; recordCount++)
                    {
                        var currentRecordIndex = ((Recordsizeinbytes) * recordCount);
                        FileStream.Seek(Metadataoffsetinbytes + currentRecordIndex, SeekOrigin.Begin);
                        FileStream.Read(bytedata, 0, bytedata.Length);
                        Bitmap image = new Bitmap(Image_width, Image_hight);

                        string Lable = null;
                        string Lable_Clean = null;
                        byte[] labelByte = new byte[Lablesizeinbytes];
                        //ofsett to read lable 
                        FileStream.Seek(Lableoffset + currentRecordIndex, SeekOrigin.Begin);
                        FileStream.Read(labelByte, 0, labelByte.Length);
                        //Lable = System.Text.Encoding.ASCII.GetString(labelByte).Trim();
                        Lable = getLable(labelByte);
                        Lable_Clean = CleanFileName(Lable);
                        int colourChunk = 8 / m_colourdepthinbits;

                        for (int y = 0; y < image.Height; y++)
                        {
                            for (int x = 0; x < image.Width; x += colourChunk)
                            {
                                var arrayindex = ((x + y) + ((image.Width - 1) * y)) / colourChunk;
                                var individualColourValuelist = ConvertByteToNibbles(bytedata[arrayindex], m_colourdepthinbits);
                                for (int j = 0; j < individualColourValuelist.Length; j++)
                                {
                                    int GrayscaleValue = individualColourValuelist[j] * ColourLevels;
                                    Color currentColour = Color.FromArgb(GrayscaleValue, GrayscaleValue, GrayscaleValue);
                                    image.SetPixel(x + j, y, currentColour);

                                }


                            }
                            //Console.WriteLine(y);
                        }
                        if (SaveTask != null)
                        {
                            await SaveTask;

                            // Console.WriteLine("Image Saved");
                        }
                        var newdirectory = Directory.CreateDirectory(saveFolderPath + "/img/" + Lable_Clean);
                        var cropArea = new Rectangle(image_offset, image_offset, 96, 96);
                        var imageToSave = tocrop == true ? image.Clone(cropArea, image.PixelFormat) : image;
                        SaveTask = Task.Run(() => imageToSave.Save(newdirectory.FullName + "\\Dataset" + Path.GetFileName(currentFile) + "- " + recordCount + ".jpg", ImageFormat.Jpeg));
                    }
                }

                //catch (Exception e)
                //{
                //	Console.WriteLine(e);
                //	Console.WriteLine("error in " + currentFile);

                //}

            }

        }

        static public void Main(string[] args)
        {
            // this works for datasets  1, 6, 7 8, 9g 9d 
            //const int sizeofRecordinbytes = 8192;
            //const int Width = 128;
            // const int Height = 127;
            // const int metadataoffsetInbytes = 64;
            // const int lableoffsetInbytes = 2;

            //const int packageSize = sizeofRecordinbytes - metadataoffsetInbytes;
            // const int LableSizeinBytes = 2;
            //Encoding lableJISEncoder = Encoding.GetEncoding("EUC-JP");

            //var db9 = new DataBaseSettings(7, 8192, 128, 127, 64, 2, 2, Encoding.GetEncoding("EUC-JP"), 4);
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            //db9.ExtractDataset(@"D:\Datasets\ETL9G", @"D:\Datasets\ETL8B", true).Wait();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            // Data.ExtractDataset(@"E:\kanji dataset\ETL8B\ETL8B", @"E:\kanji dataset\decode\ETL8B", false).Wait();
            var files = Directory.GetFiles(@"E:\kanji dataset\").Where(x => Path.GetExtension(x) == ".zip");
            Parallel.ForEach(files, (currentFile) =>
            {
                var destination = Path.Combine(Path.GetDirectoryName(currentFile), "extract");
                var folderName = Path.GetFileNameWithoutExtension(currentFile);
                var createdFile = Path.Combine(destination, folderName);
                if (Directory.Exists(createdFile) == false)
                {
                    ZipFile.ExtractToDirectory(currentFile, destination);
                }
                var decodeLocation = @"E:\kanji dataset\decode\";
                DataBaseSettings Data = null;
                switch (folderName)
                {
                    case "ETL2":
                        var f = Directory.GetFiles(createdFile);
                        foreach (var file in f)
                        {
                            var pythonArgs = string.Format("\"{0}\" \"{1}\" \"{2}\"",
                        @"E:\Programming\c#\ETL_Binary_File_Paser\Database2Runner.py",
                        file, @"E:\kanji dataset\decode\img");
                            run_cmd("python", pythonArgs);
                        }
                        //   Data = new DataBaseSettings(0, 2952, 72, 76,288 , 24,
                        //          4, labelencoding: Encoding.GetEncoding("EUC-JP"), 4);
                        break;


                    case "ETL3":
                    case "ETL4":
                    case "ETL5":
                    throw new NotImplementedException();
                        break;

                    case "ETL1":
                    case "ETL6":
                    case "ETL7":
                    //  Data = new DataBaseSettings(3, 2052, 64, 63, 32, 2,
                    //         2, labelencoding: Encoding.GetEncoding("ASCII"), 4);
                    //     break;
                    case "ETL8G":
                        // Data = new DataBaseSettings(10, 8199, 128, 127, 60, 2,
                        //     2, labelencoding: Encoding.GetEncoding("EUC-JP"), 4);
                        break;
                    case "ETL8B":
                        // Data = new DataBaseSettings(0, 512, 64, 63, 8, 2,
                        // 2, labelencoding: Encoding.GetEncoding("EUC-JP"), 1);
                        break;
                    case "ETL9G":
                        // Data = new DataBaseSettings(6, 8199, 128, 127, 8, 2,
                        //      2, Encoding.GetEncoding("EUC-JP"), 4);
                        break;
                    case "ETL9B":
                        // Data = new DataBaseSettings(63, 576, 64, 63, 8, 2,
                        //      2, labelencoding: Encoding.GetEncoding("EUC-JP"), 1);
                        break;
                }
                if (Data != null)
                {
                    Data.ExtractDataset(createdFile, decodeLocation, false).Wait();
                }


            });
        }
        private static void run_cmd(string cmd, string args)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = cmd;
            start.Arguments = args;
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            using (Process process = Process.Start(start))

            using (StreamReader reader = process.StandardOutput)
            {
                string result = reader.ReadToEnd();
                Console.Write(result);
            }
        }


        private static string CleanFileName(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty)).Trim();
        }


        public static (int highNibble, int lowNibble) ConvertByteToNibbles(byte DataToConvert)
        {
            // 15 =  00001111 so you are removing the first 4 bits in the array to get the first nibble
            int lowNibble = DataToConvert & 15;
            // you then shift the first 4 bits off the data then repeat the prosess 
            int highNibble = (DataToConvert >> 4) & 15;
            return (highNibble, lowNibble);
        }

        public static int[] ConvertByteToNibbles(byte DataToConvert, int colourdepthinbits)
        {
            int partCount = 8 / colourdepthinbits;
            List<int> parts = new List<int>();
            for (int i = 0; i < partCount; i++)
            {

                int part = (DataToConvert >> i) & (int)(Math.Pow(2, colourdepthinbits) - 1);
                parts.Add(part);

            }
            parts.Reverse();
            return parts.ToArray();
        }





    }
}
