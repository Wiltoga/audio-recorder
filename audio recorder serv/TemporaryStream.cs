using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace audioRecorderServ
{
    public class TemporaryStream : Stream
    {
        #region Private Fields

        private LinkedListNode<Block> activeBlock;
        private int activeCursor;
        private LinkedList<Block> blocks;

        private int numberOfBlocks;
        private long position;
        private long size;

        #endregion Private Fields

        #region Public Constructors

        public TemporaryStream(int maxBlockCount, int blockSize = 1024)
        {
            if (maxBlockCount <= 0)
                throw new ArgumentException("maxBlockCount can not be less than 1");
            MaxBlockCount = maxBlockCount;
            BlockSize = blockSize;
            blocks = new LinkedList<Block>();
            activeBlock = blocks.AddLast(new Block(BlockSize));
            activeCursor = 0;
            size = 0;
            position = 0;
            numberOfBlocks = 1;
        }

        #endregion Public Constructors

        #region Public Properties

        public int BlockSize { get; private set; }
        public override bool CanRead => true;

        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => size;
        public int MaxBlockCount { get; private set; }
        public override long Position { get => position; set => Seek(value, SeekOrigin.Begin); }

        #endregion Public Properties

        #region Public Methods

        public void Clear()
        {
            blocks.Clear();
            activeBlock = blocks.AddLast(new Block(BlockSize));
            activeCursor = 0;
            size = 0;
            position = 0;
            numberOfBlocks = 1;
        }

        public override void Close()
        {
            base.Close();
            blocks.Clear();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (position == size)
                return 0;
            int readBytes = 0;
            while (readBytes < count)
            {
                buffer[readBytes + offset] = activeBlock.Value.bytes[activeCursor];
                readBytes++;
                activeCursor++;
                position++;
                if (activeCursor == activeBlock.Value.used)
                    if (position != size)
                    {
                        activeBlock = activeBlock.Next;
                        activeCursor = 0;
                    }
                    else
                        break;
            }
            return readBytes;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                position = 0;
                activeCursor = 0;
                activeBlock = blocks.First;
            }
            else if (origin == SeekOrigin.End)
            {
                position = size;
                activeBlock = blocks.Last;
                activeCursor = activeBlock.Value.used;
            }
            if (offset > 0)
                for (int i = 0; i < offset;)
                {
                    activeCursor++;
                    i++;
                    position++;
                    if (activeCursor == activeBlock.Value.used)
                    {
                        if (activeBlock.Next != null)
                            activeBlock = activeBlock.Next;
                        else
                            break;
                        activeCursor = 0;
                    }
                }
            else
                for (int i = 0; i > offset;)
                {
                    if (activeCursor == 0)
                    {
                        if (activeBlock.Previous != null)
                            activeBlock = activeBlock.Previous;
                        else
                            break;
                        activeCursor = activeBlock.Value.used;
                    }
                    activeCursor--;
                    i--;
                    position--;
                }
            return position;
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                activeBlock.Value.bytes[activeCursor] = buffer[i + offset];
                position++;
                activeCursor++;
                if (activeBlock.Value.used < activeCursor)
                {
                    activeBlock.Value.used = activeCursor;
                    size++;
                }
                if (activeCursor == activeBlock.Value.bytes.Length)
                {
                    if (activeBlock.Next == null)
                    {
                        activeBlock = blocks.AddLast(new Block(BlockSize));
                        numberOfBlocks++;
                        if (numberOfBlocks > MaxBlockCount)
                        {
                            var bytesRemoved = blocks.First.Value.used;
                            position -= bytesRemoved;
                            size -= bytesRemoved;
                            blocks.First.Value.bytes = null;
                            blocks.RemoveFirst();
                        }
                    }
                    else
                        activeBlock = activeBlock.Next;

                    activeCursor = 0;
                }
            }
        }

        #endregion Public Methods

        #region Private Classes

        private class Block
        {
            #region Public Constructors

            public Block(int size)
            {
                bytes = new byte[size];
                used = 0;
            }

            #endregion Public Constructors

            #region Public Properties

            public byte[] bytes { get; set; }

            public int used { get; set; }

            #endregion Public Properties
        }

        #endregion Private Classes
    }
}