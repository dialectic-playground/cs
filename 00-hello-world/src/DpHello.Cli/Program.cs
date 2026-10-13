using DialecticPlayground.Hello;

// Thin I/O shell: hand the arguments and the raw (binary) standard streams to the
// application. Console.Out is not used: it would encode with the console code page.
// The runtime has already converted the arguments to strings: from UTF-8 bytes on POSIX and
// from the UTF-16 command line on Windows (CONV-05).
using var stdout = Console.OpenStandardOutput();
using var stderr = Console.OpenStandardError();
return Application.Run(args, stdout, stderr);
