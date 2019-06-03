using GitHub.Services.Common.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;

namespace GitHub.Services.Common
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ContentIdentification : IDisposable
    {
        // consider moving the framework ByteArray class to common or webapi so we can use it here

        private const Int32 m_pagesPerBlock = 32;
        private const Int32 m_bytesPerPage = 64 * 1024;

        private byte[] m_blockBuffer;
        private byte[] m_pageBuffer;

        public ContentIdentification(int pageSize = m_bytesPerPage, int pagesPerBlock = m_pagesPerBlock)
            : this(new byte[pageSize * pagesPerBlock], new byte[pageSize], pageSize, pagesPerBlock)
        {
            PageSize = pageSize;
            PagesPerBlock = pagesPerBlock;
        }

        public ContentIdentification(byte[] blockBuffer, byte[] pageBuffer, int pageSize = m_bytesPerPage, int pagesPerBlock = m_pagesPerBlock)
        {
            m_blockBuffer = blockBuffer;
            m_pageBuffer = pageBuffer;

            PageSize = pageSize;
            PagesPerBlock = pagesPerBlock;
        }

        public int PageSize { get; private set; }
        public int PagesPerBlock { get; private set; }

        public int BlockSize
        {
            get
            {
                return PageSize * PagesPerBlock;
            }
        }

        public byte[] CalculateContentIdentifier(Stream blocks, Boolean includesFinalBlock, byte[] startingContentIdentifier = null)
        {
            byte[] rollingContentIdentifier;
            byte[] savedRollingIdentifier;
            if (startingContentIdentifier == null)
            {
                rollingContentIdentifier = m_startingConstant;
            }
            else
            {
                rollingContentIdentifier = startingContentIdentifier;
            }

            int bytesRead = 0;
            int lastBlockSize = 0;
            byte[] blockIdentifier;
            bytesRead = blocks.Read(m_blockBuffer, 0, BlockSize);

            do
            {
                lastBlockSize = bytesRead;
                savedRollingIdentifier = rollingContentIdentifier;
                blockIdentifier = CalculateSingleBlockIdentifier(m_blockBuffer, bytesRead);
                rollingContentIdentifier = CalculateRollingBlockIdentifier(blockIdentifier, rollingContentIdentifier, false);

                bytesRead = blocks.Read(m_blockBuffer, 0, BlockSize);
            }
            while (bytesRead > 0) ;

            if (includesFinalBlock)
            {
                // since we have a final block need to recalculate final block with true
                rollingContentIdentifier = CalculateRollingBlockIdentifier(blockIdentifier, savedRollingIdentifier, true);
            }
            else if (lastBlockSize != BlockSize)
            {
                // we have a non-final block which is a partial block
                throw new ArgumentException(CommonResources.ContentIdCalculationBlockSizeError(BlockSize), "blocks");
            }

            return rollingContentIdentifier;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public byte[] CalculateContentIdentifier(byte[] block, Boolean isFinalBlock, byte[] startingContentIdentifier = null)
        {
            if ((block.Length != this.BlockSize && !isFinalBlock) || (block.Length > this.BlockSize && isFinalBlock))
            {
                throw new ArgumentException(CommonResources.ContentIdCalculationBlockSizeError(BlockSize), "block");
            }

            byte[] rollingContentIdentifier;
            if (startingContentIdentifier == null)
            {
                rollingContentIdentifier = m_startingConstant;
            }
            else
            {
                rollingContentIdentifier = startingContentIdentifier;
            }

            byte[] blockIdentifier;

            blockIdentifier = CalculateSingleBlockIdentifier(block, block.Length);
            return CalculateRollingBlockIdentifier(blockIdentifier, rollingContentIdentifier, isFinalBlock);
        }

        private byte[] CalculateRollingBlockIdentifier(byte[] currentBlockIdentifier, byte[] previousRollingIdentifier, Boolean isFinalBlock)
        {
            List<byte> resultBuffer = new List<byte>(previousRollingIdentifier);
            resultBuffer.AddRange(currentBlockIdentifier);
            resultBuffer.Add(Convert.ToByte(isFinalBlock));

            return CalculateHash(resultBuffer);
        }

        private byte[] CalculateSingleBlockIdentifier(byte[] block, int blockLength)
        {
            int bytesToCopy = 0;
            int pageCounter = 0;
            List<byte> pageIdentifiersBuffer = new List<byte>();

            while (blockLength > pageCounter * PageSize)
            {
                bytesToCopy = Math.Min(blockLength - (pageCounter * PageSize), PageSize);
                Array.Copy(block, pageCounter * PageSize, m_pageBuffer, 0, bytesToCopy);
                byte[] pageHash = CalculateHash(m_pageBuffer, bytesToCopy);
                pageCounter++;
                pageIdentifiersBuffer.AddRange(pageHash);

                if (pageCounter > PagesPerBlock)
                {
                    throw new ArgumentException(CommonResources.ContentIdCalculationBlockSizeError(BlockSize), "block");
                }
            }

            //calculate the block buffer as we have make pages or have a partial page
            return CalculateHash(pageIdentifiersBuffer);
        }

        private byte[] CalculateHash(byte[] buffer, int count)
        {
            return m_hashProvider.ComputeHash(buffer, 0, count);
        }

        private byte[] CalculateHash(List<byte> buffer)
        {
            return m_hashProvider.ComputeHash(buffer.ToArray(), 0, buffer.Count);
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.m_hashProvider != null)
                {
                    this.m_hashProvider.Dispose();
                }
            }
        }

        private byte[] m_startingConstant = System.Text.ASCIIEncoding.ASCII.GetBytes("VSO Content Identifier Seed");

        private SHA256CryptoServiceProvider m_hashProvider = new SHA256CryptoServiceProvider();

    }
}
