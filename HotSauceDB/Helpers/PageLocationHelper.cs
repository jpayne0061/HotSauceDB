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
            using (FileStream fileStream = new FileStream(Globals.FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fileStream.Position = pageAddress + Globals.NextPointerAddress;

                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    return binaryReader.ReadInt64();
                }
            }
        }

        public static long GetNextPagePointerFromCurrentPosition(long currentStreamPostition)
        {
            return GetNextPagePointer(GetCurrentPageAddress(currentStreamPostition));
        }

        public static long GetCurrentPageAddress(long currentStreamPostition)
        {
            return currentStreamPostition - (currentStreamPostition % Globals.PageSize);
        }

    }
}
