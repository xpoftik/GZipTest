# GZipTest
Multithread archiver

Usage: GZipTest [options]

Options:
  -m|--mode <MODE>                        Compress/Decompress
  -t|--target-filename <TARGET_FILENAME>  Target filename
  -o|--output-filename <OUTPUT_FILENAME>  Output filename
  -?|-h|--help
  
Compress:
GZipTest.exe -m compress -t target_filename -o output_filename

Decompress: 
GZipTest.exe -m decompress -t target_filename -o output_filename

Archiver is built on classical pattern pub/sub.
Compressor and decompressor implemented as a decorator and just wrap internal stream. Every async method imlemented as a state machine, as a result we have non blocking async operations. Threads count depends on count of processors by default, and may be adjusted whenever you need.
