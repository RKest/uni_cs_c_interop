using System.Diagnostics;
using System.Runtime.InteropServices;
using WebApplication1;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();
var tmp = Path.GetTempPath();
var src = Path.Join(tmp, "src.c");

const int RTLD_NOW = 2;
[DllImport("libc.so.6")]
static extern IntPtr dlopen(string filename, int flags);
[DllImport("libc.so.6")]
static extern IntPtr dlsym(IntPtr handle, string symbol);

int iter = 0;

app.MapPost("/run", async context =>
{
    var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    var file = File.Create(src);
    var w = new StreamWriter(file);
    w.WriteLine("#include <string.h>");
    w.WriteLine("char* foo(){");
    w.WriteLine(body);
    w.WriteLine("}");
    w.Close();
    
    var proc1 = new ProcessStartInfo();
    proc1.UseShellExecute = true;
    proc1.WorkingDirectory = tmp;
    proc1.FileName = "gcc";
    proc1.Arguments = $"-fPIC -shared -o foo{++iter}.so {src}";
    Process.Start(proc1);
});

app.MapGet("/run", async context =>
{
    var lib = dlopen(Path.Join(tmp, $"foo{iter}.so"), RTLD_NOW);
    var sym = dlsym(lib, "foo");
    var func = Marshal.GetDelegateForFunctionPointer<StringDelegate>(sym);
    await context.Response.WriteAsync(func());
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();
app.Run();