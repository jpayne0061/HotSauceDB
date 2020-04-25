using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpDb.Helpers
{
    public static class PageLocationHelper
    {
        public static long GetNextDivisbleNumber(long current, long divisor)
        {
            long dividend = current / divisor;

            long next = divisor * (dividend + 1);

            return next;
        }

        public static long GetNextPagePointer(long pageAddress)
        {
            using (FileStream fileStream = File.OpenRead(Globals.FILE_NAME))
            {
                fileStream.Position = pageAddress + Globals.NextPointerAddress;

                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    return binaryReader.ReadInt64();
                }
            }
        }
    }
}
