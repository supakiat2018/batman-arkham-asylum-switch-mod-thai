using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

class Tegra
{
    static int GetGobOffset(int gx, int gy)
    {
        return (gx & 0x0F) |
               ((gy & 1) << 4) |
               (((gx >> 4) & 1) << 5) |
               (((gy >> 1) & 1) << 6) |
               (((gy >> 2) & 1) << 7) |
               (((gx >> 5) & 1) << 8);
    }

    public static byte[] SwizzleDxt5(byte[] linear, int w, int h)
    {
        int bw = w / 4;
        int bh = h / 4;
        int bytesPerBlock = 16;
        int gobW = 4;
        int gobH = 8;
        int gobSize = 512;

        int blockHeight = Math.Min(16, Math.Max(1, h / 32));
        int blockH = gobH * blockHeight;
        int blockSize = gobSize * blockHeight;

        int widthInGobs = (bw + gobW - 1) / gobW;
        int heightInBlocks = (bh + blockH - 1) / blockH;

        byte[] swizzled = new byte[widthInGobs * heightInBlocks * blockSize];

        for (int by = 0; by < heightInBlocks; by++)
        {
            for (int bx = 0; bx < widthInGobs; bx++)
            {
                for (int gy = 0; gy < blockHeight; gy++)
                {
                    int blockStart = (by * widthInGobs + bx) * blockSize + gy * gobSize;
                    int yBase = by * blockH + gy * gobH;
                    int xBase = bx * gobW;

                    for (int yInGob = 0; yInGob < 8; yInGob++)
                    {
                        int blockY = yBase + yInGob;
                        if (blockY >= bh) continue;

                        for (int xInGob = 0; xInGob < gobW; xInGob++)
                        {
                            int blockX = xBase + xInGob;
                            if (blockX >= bw) continue;

                            int gx = xInGob * bytesPerBlock;
                            int offsetInGob = GetGobOffset(gx, yInGob);

                            int srcIdx = (blockY * bw + blockX) * bytesPerBlock;
                            int dstIdx = blockStart + offsetInGob;

                            Buffer.BlockCopy(linear, srcIdx, swizzled, dstIdx, bytesPerBlock);
                        }
                    }
                }
            }
        }
        return swizzled;
    }

    public static byte[] DeswizzleDxt5(byte[] swizzled, int w, int h)
    {
        int bw = w / 4;
        int bh = h / 4;
        int bytesPerBlock = 16;
        int gobW = 4;
        int gobH = 8;
        int gobSize = 512;

        int blockHeight = Math.Min(16, Math.Max(1, h / 32));
        int blockH = gobH * blockHeight;
        int blockSize = gobSize * blockHeight;

        int widthInGobs = (bw + gobW - 1) / gobW;
        int heightInBlocks = (bh + blockH - 1) / blockH;

        byte[] linear = new byte[bw * bh * bytesPerBlock];

        for (int by = 0; by < heightInBlocks; by++)
        {
            for (int bx = 0; bx < widthInGobs; bx++)
            {
                for (int gy = 0; gy < blockHeight; gy++)
                {
                    int blockStart = (by * widthInGobs + bx) * blockSize + gy * gobSize;
                    int yBase = by * blockH + gy * gobH;
                    int xBase = bx * gobW;

                    for (int yInGob = 0; yInGob < 8; yInGob++)
                    {
                        int blockY = yBase + yInGob;
                        if (blockY >= bh) continue;

                        for (int xInGob = 0; xInGob < gobW; xInGob++)
                        {
                            int blockX = xBase + xInGob;
                            if (blockX >= bw) continue;

                            int gx = xInGob * bytesPerBlock;
                            int offsetInGob = GetGobOffset(gx, yInGob);

                            int srcIdx = blockStart + offsetInGob;
                            int dstIdx = (blockY * bw + blockX) * bytesPerBlock;

                            Buffer.BlockCopy(swizzled, srcIdx, linear, dstIdx, bytesPerBlock);
                        }
                    }
                }
            }
        }
        return linear;
    }
}
