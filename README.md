# GZipTest
Multithread archiver

Usage: GZipTest [options]

Options:
  -m|--mode <MODE>                        Compress/Decompress
  -t|--target-filename <TARGET_FILENAME>  Target filename
  -o|--output-filename <OUTPUT_FILENAME>  Output filename
  -?|-h|--help
  
Compress:
GZipTest.exe -m compress -t <target filename> -o <output filename>

Decompress: 
GZipTest.exe -m decompress -t <target filename> -o <output filename>

Archiver is built on classical pattern pub/sub.
Compressor and decompressor implemented as a decorator and just wrap internal stream.
