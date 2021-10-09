using System.IO;

namespace HotSauceDb.Helpers
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
            using (FileStream fileStream = new FileStream(Constants.FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fileStream.Position = pageAddress + Constants.Next_Pointer_Address;

                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    return binaryReader.ReadInt64();
                }
            }
        }

        public static long GetCurrentPageAddress(long currentStreamPostition)
        {
            return currentStreamPostition - (currentStreamPostition % Constants.Page_Size);
        }

    }
}
