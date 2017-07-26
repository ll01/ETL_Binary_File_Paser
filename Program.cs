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


namespace openfile
{
	static class Program
	{
		class DataBaseSettings
		{

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
				m_colourLevels = (int)(256 / Math.Pow(colourdepthinbits, 2));
			}

			public string getLable(byte[] lableByteArray)
			{
				string Lable = null;
				switch (m_labelencoding.BodyName.ToUpper())
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

		}

		static void Main(string[] args)
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

			var db9 = new DataBaseSettings(7, 8192, 128, 127, 64, 2, 2, Encoding.GetEncoding("EUC-JP"), 4);
			const int white = 255;


			var FileStream = File.Open(@"C:\Users\enti2\Desktop\Datasets\ETL9G_01", FileMode.Open);
			//var FileStream = File.Open(@"D:\Downloads\ETL1\ETL1\ETL1C_01", FileMode.Open);
			byte[] bytedata = new byte[db9.Recordsizeinbytes];


			List<string> lables = new List<string>();
			//read file into byte array offseting 33 (for the meta data)

			//convert to bitarray since the colours are stored as  4 bit collour depth
			for (int recordCount = 0; recordCount < FileStream.Length / db9.Recordsizeinbytes; recordCount++)
			{
				var currentRecordIndex = ((db9.Recordsizeinbytes + db9.Offsetinbytes) * recordCount);
				FileStream.Seek(db9.Metadataoffsetinbytes + currentRecordIndex, SeekOrigin.Begin);
				FileStream.Read(bytedata, 0, bytedata.Length);
				Bitmap image = new Bitmap(db9.Image_width, db9.Image_hight);
				int i = 0;
				string Lable = null;
				string Lable_Clean = null;

				byte[] labelByte = new byte[db9.Lablesizeinbytes];
				//ofsett to read lable 
				FileStream.Seek(db9.Lableoffset + currentRecordIndex, SeekOrigin.Begin);
				FileStream.Read(labelByte, 0, labelByte.Length);
				//Lable = System.Text.Encoding.ASCII.GetString(labelByte).Trim();
				Lable = db9.getLable(labelByte);
				Lable_Clean = CleanFileName(Lable);

				for (int y = 0; y < db9.Image_hight; y++)
				{
					for (int x = 0; x < db9.Image_width; x += 2)
					{

						var byteNibbles = ConvertByteToNibbles(bytedata[i]);
						// we multiply by 16 because the colour deaps in the package is 16 and bitmap is 256 per channel  256 /16 = 16
						int lowNibble = white - (byteNibbles.lowNibble * db9.ColourLevels);
						//high nibble
						int highNibble = white - (byteNibbles.highNibble * db9.ColourLevels);
						Color currentColour = Color.FromArgb(lowNibble, lowNibble, lowNibble);
						image.SetPixel(x, y, currentColour);

						currentColour = Color.FromArgb(highNibble, highNibble, highNibble);
						image.SetPixel(x + 1, y, currentColour);
						i++;
					}
				}

				image.Save("img/"+ Lable_Clean + "Record- " + recordCount + ".jpg", ImageFormat.Jpeg);
			}

		}

		private static string CleanFileName(string fileName)
		{
			return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
		}


		public static (int highNibble, int lowNibble) ConvertByteToNibbles(byte DataToConvert)
		{
			// 15 =  00001111 so you are removing the first 4 bits in the array to get the first nibble
			int lowNibble = DataToConvert & 15;
			// you then shift the first 4 bits off the data then repeat the prosess 
			int highNibble = (DataToConvert >> 4) & 15;
			return (highNibble, lowNibble);
		}





	}
}