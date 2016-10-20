using System.Reflection;
using System.Runtime.CompilerServices;
using Android.App;


[assembly: AssemblyTitle ("EchoAndroid")]
[assembly: AssemblyDescription ("")]
[assembly: AssemblyConfiguration ("")]
[assembly: AssemblyCompany ("")]
[assembly: AssemblyProduct ("")]
[assembly: AssemblyCopyright ("Mike Laberko")]
[assembly: AssemblyTrademark ("")]
[assembly: AssemblyCulture ("")]


[assembly: AssemblyVersion ("1.0.0")]

#if DEBUG
[assembly: Application(Debuggable=true)]
#else
[assembly: Application(Debuggable = false)]
#endif