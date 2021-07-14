//          Copyright Tao Klerks, 2010-2012, tao@klerks.biz         
//          Licensed under the modified BSD license.


using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace FastColoredTextBoxNS
{
    public static class EncodingDetector
    {
        private const long defaultHeuristicSampleSize = 0x10000; //completely arbitrary - inappropriate for high numbers of files / high speed requirements

        public static Encoding? DetectTextFileEncoding(string inputFilename)
        {
            using FileStream textfileStream = File.OpenRead(inputFilename);
            return DetectTextFileEncoding(textfileStream, defaultHeuristicSampleSize);
        }

        public static Encoding? DetectTextFileEncoding(FileStream inputFileStream, long heuristicSampleSize)
        {
            return DetectTextFileEncoding(inputFileStream, defaultHeuristicSampleSize, out bool _);
        }

        public static Encoding? DetectTextFileEncoding(FileStream inputFileStream, long heuristicSampleSize, out bool hasBOM)
        {
            long originalPos = inputFileStream.Position;

            inputFileStream.Position = 0;


            //First read only what we need for BOM detection
            byte[] bomBytes = new byte[inputFileStream.Length > 4 ? 4 : inputFileStream.Length];
            _ = inputFileStream.Read(bomBytes, 0, bomBytes.Length);

            Encoding? encodingFound = DetectBOMBytes(bomBytes);

            if (encodingFound != null)
            {
                inputFileStream.Position = originalPos;
                hasBOM = true;
                return encodingFound;
            }


            //BOM Detection failed, going for heuristics now.
            //  create sample byte array and populate it
            byte[] sampleBytes = new byte[heuristicSampleSize > inputFileStream.Length ? inputFileStream.Length : heuristicSampleSize];
            Array.Copy(bomBytes, sampleBytes, bomBytes.Length);
            if (inputFileStream.Length > bomBytes.Length)
                _ = inputFileStream.Read(sampleBytes, bomBytes.Length, sampleBytes.Length - bomBytes.Length);
            inputFileStream.Position = originalPos;

            //test byte array content
            encodingFound = DetectUnicodeInByteSampleByHeuristics(sampleBytes);

            hasBOM = false;
            return encodingFound;
        }

        public static Encoding? DetectBOMBytes(byte[] bOMBytes)
        {
            if (bOMBytes.Length < 2)
                return null;

            if (bOMBytes[0] == 0xff
                && bOMBytes[1] == 0xfe
                && (bOMBytes.Length < 4
                    || bOMBytes[2] != 0
                    || bOMBytes[3] != 0
                    )
                )
                return Encoding.Unicode;

            if (bOMBytes[0] == 0xfe
                && bOMBytes[1] == 0xff
                )
                return Encoding.BigEndianUnicode;

            if (bOMBytes.Length < 3)
                return null;

            if (bOMBytes[0] == 0xef && bOMBytes[1] == 0xbb && bOMBytes[2] == 0xbf)
                return Encoding.UTF8;

            if (bOMBytes[0] == 0x2b && bOMBytes[1] == 0x2f && bOMBytes[2] == 0x76)
                return Encoding.UTF7;

            if (bOMBytes.Length < 4)
                return null;

            if (bOMBytes[0] == 0xff && bOMBytes[1] == 0xfe && bOMBytes[2] == 0 && bOMBytes[3] == 0)
                return Encoding.UTF32;

            if (bOMBytes[0] == 0 && bOMBytes[1] == 0 && bOMBytes[2] == 0xfe && bOMBytes[3] == 0xff)
                return Encoding.GetEncoding(12001);

            return null;
        }

        public static Encoding? DetectUnicodeInByteSampleByHeuristics(byte[] sampleBytes)
        {
            long oddBinaryNullsInSample = 0;
            long evenBinaryNullsInSample = 0;
            long suspiciousUTF8SequenceCount = 0;
            long suspiciousUTF8BytesTotal = 0;
            long likelyUSASCIIBytesInSample = 0;

            //Cycle through, keeping count of binary null positions, possible UTF-8 
            //  sequences from upper ranges of Windows-1252, and probable US-ASCII 
            //  character counts.

            long currentPos = 0;
            int skipUTF8Bytes = 0;

            while (currentPos < sampleBytes.Length)
            {
                //binary null distribution
                if (sampleBytes[currentPos] == 0)
                {
                    if (currentPos % 2 == 0)
                        evenBinaryNullsInSample++;
                    else
                        oddBinaryNullsInSample++;
                }

                //likely US-ASCII characters
                if (IsCommonUSASCIIByte(sampleBytes[currentPos]))
                    likelyUSASCIIBytesInSample++;

                //suspicious sequences (look like UTF-8)
                if (skipUTF8Bytes == 0)
                {
                    int lengthFound = DetectSuspiciousUTF8SequenceLength(sampleBytes, currentPos);

                    if (lengthFound > 0)
                    {
                        suspiciousUTF8SequenceCount++;
                        suspiciousUTF8BytesTotal += lengthFound;
                        skipUTF8Bytes = lengthFound - 1;
                    }
                }
                else
                {
                    skipUTF8Bytes--;
                }

                currentPos++;
            }

            //1: UTF-16 LE - in english / european environments, this is usually characterized by a 
            //  high proportion of odd binary nulls (starting at 0), with (as this is text) a low 
            //  proportion of even binary nulls.
            //  The thresholds here used (less than 20% nulls where you expect non-nulls, and more than
            //  60% nulls where you do expect nulls) are completely arbitrary.

            if ((evenBinaryNullsInSample * 2.0 / sampleBytes.Length) < 0.2
                && (oddBinaryNullsInSample * 2.0 / sampleBytes.Length) > 0.6
                )
                return Encoding.Unicode;


            //2: UTF-16 BE - in english / european environments, this is usually characterized by a 
            //  high proportion of even binary nulls (starting at 0), with (as this is text) a low 
            //  proportion of odd binary nulls.
            //  The thresholds here used (less than 20% nulls where you expect non-nulls, and more than
            //  60% nulls where you do expect nulls) are completely arbitrary.

            if ((oddBinaryNullsInSample * 2.0 / sampleBytes.Length) < 0.2
                && (evenBinaryNullsInSample * 2.0 / sampleBytes.Length) > 0.6
                )
                return Encoding.BigEndianUnicode;


            //3: UTF-8 - Martin Dürst outlines a method for detecting whether something CAN be UTF-8 content 
            //  using regexp, in his w3c.org unicode FAQ entry: 
            //  http://www.w3.org/International/questions/qa-forms-utf-8
            //  adapted here for C#.
            string potentiallyMangledString = Encoding.ASCII.GetString(sampleBytes);
            Regex uTF8Validator = new Regex(@"\A("
                + @"[\x09\x0A\x0D\x20-\x7E]"
                + @"|[\xC2-\xDF][\x80-\xBF]"
                + @"|\xE0[\xA0-\xBF][\x80-\xBF]"
                + @"|[\xE1-\xEC\xEE\xEF][\x80-\xBF]{2}"
                + @"|\xED[\x80-\x9F][\x80-\xBF]"
                + @"|\xF0[\x90-\xBF][\x80-\xBF]{2}"
                + @"|[\xF1-\xF3][\x80-\xBF]{3}"
                + @"|\xF4[\x80-\x8F][\x80-\xBF]{2}"
                + @")*\z");
            if (uTF8Validator.IsMatch(potentiallyMangledString))
            {
                //Unfortunately, just the fact that it CAN be UTF-8 doesn't tell you much about probabilities.
                //If all the characters are in the 0-127 range, no harm done, most western charsets are same as UTF-8 in these ranges.
                //If some of the characters were in the upper range (western accented characters), however, they would likely be mangled to 2-byte by the UTF-8 encoding process.
                // So, we need to play stats.

                // The "Random" likelihood of any pair of randomly generated characters being one 
                //   of these "suspicious" character sequences is:
                //     128 / (256 * 256) = 0.2%.
                //
                // In western text data, that is SIGNIFICANTLY reduced - most text data stays in the <127 
                //   character range, so we assume that more than 1 in 500,000 of these character 
                //   sequences indicates UTF-8. The number 500,000 is completely arbitrary - so sue me.
                //
                // We can only assume these character sequences will be rare if we ALSO assume that this
                //   IS in fact western text - in which case the bulk of the UTF-8 encoded data (that is 
                //   not already suspicious sequences) should be plain US-ASCII bytes. This, I 
                //   arbitrarily decided, should be 80% (a random distribution, eg binary data, would yield 
                //   approx 40%, so the chances of hitting this threshold by accident in random data are 
                //   VERY low). 

                if ((suspiciousUTF8SequenceCount * 500000.0 / sampleBytes.Length >= 1) //suspicious sequences
                    && (
                    //all suspicious, so cannot evaluate proportion of US-Ascii
                           sampleBytes.Length - suspiciousUTF8BytesTotal == 0
                           ||
                           likelyUSASCIIBytesInSample * 1.0 / (sampleBytes.Length - suspiciousUTF8BytesTotal) >= 0.8
                       )
                    )
                    return Encoding.UTF8;
            }

            return null;
        }

        private static bool IsCommonUSASCIIByte(byte testByte)
        {
            if (testByte is 0x0A //lf
                or 0x0D //cr
                or 0x09 //tab
                or >= 0x20 and <= 0x2F or >= 0x30 and <= 0x39 or >= 0x3A and <= 0x40 or >= 0x41 and <= 0x5A or >= 0x5B and <= 0x60 or >= 0x61 and <= 0x7A or >= 0x7B and <= 0x7E)
                return true;
            else
                return false;
        }

        private static int DetectSuspiciousUTF8SequenceLength(byte[] sampleBytes, long currentPos)
        {
            int lengthFound = 0;

            if (sampleBytes.Length >= currentPos + 1
                && sampleBytes[currentPos] == 0xC2
                )
            {
                if (sampleBytes[currentPos + 1] is 0x81
                    or 0x8D
                    or 0x8F
                    )
                    lengthFound = 2;
                else if (sampleBytes[currentPos + 1] is 0x90
                    or 0x9D
                    )
                    lengthFound = 2;
                else if (sampleBytes[currentPos + 1] is >= 0xA0
                    and <= 0xBF
                    )
                    lengthFound = 2;
            }
            else if (sampleBytes.Length >= currentPos + 1
                && sampleBytes[currentPos] == 0xC3
                )
            {
                if (sampleBytes[currentPos + 1] is >= 0x80
                    and <= 0xBF
                    )
                    lengthFound = 2;
            }
            else if (sampleBytes.Length >= currentPos + 1
                && sampleBytes[currentPos] == 0xC5
                )
            {
                if (sampleBytes[currentPos + 1] is 0x92
                    or 0x93
                    )
                    lengthFound = 2;
                else if (sampleBytes[currentPos + 1] is 0xA0
                    or 0xA1
                    )
                    lengthFound = 2;
                else if (sampleBytes[currentPos + 1] is 0xB8
                    or 0xBD
                    or 0xBE
                    )
                    lengthFound = 2;
            }
            else if (sampleBytes.Length >= currentPos + 1
                && sampleBytes[currentPos] == 0xC6
                )
            {
                if (sampleBytes[currentPos + 1] == 0x92)
                    lengthFound = 2;
            }
            else if (sampleBytes.Length >= currentPos + 1
                && sampleBytes[currentPos] == 0xCB
                )
            {
                if (sampleBytes[currentPos + 1] is 0x86
                    or 0x9C
                    )
                    lengthFound = 2;
            }
            else if (sampleBytes.Length >= currentPos + 2
                && sampleBytes[currentPos] == 0xE2
                )
            {
                if (sampleBytes[currentPos + 1] == 0x80)
                {
                    if (sampleBytes[currentPos + 2] is 0x93
                        or 0x94
                        )
                        lengthFound = 3;
                    if (sampleBytes[currentPos + 2] is 0x98
                        or 0x99
                        or 0x9A
                        )
                        lengthFound = 3;
                    if (sampleBytes[currentPos + 2] is 0x9C
                        or 0x9D
                        or 0x9E
                        )
                        lengthFound = 3;
                    if (sampleBytes[currentPos + 2] is 0xA0
                        or 0xA1
                        or 0xA2
                        )
                        lengthFound = 3;
                    if (sampleBytes[currentPos + 2] == 0xA6)
                        lengthFound = 3;
                    if (sampleBytes[currentPos + 2] == 0xB0)
                        lengthFound = 3;
                    if (sampleBytes[currentPos + 2] is 0xB9
                        or 0xBA
                        )
                        lengthFound = 3;
                }
                else if (sampleBytes[currentPos + 1] == 0x82
                    && sampleBytes[currentPos + 2] == 0xAC
                    )
                    lengthFound = 3;
                else if (sampleBytes[currentPos + 1] == 0x84
                    && sampleBytes[currentPos + 2] == 0xA2
                    )
                    lengthFound = 3;
            }

            return lengthFound;
        }
    }
}
