using BUTR.NativeAOT.Shared;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Unicode;

namespace ModInstaller.Native.Tests.Utils;

internal static partial class Utils2
{
#if DEBUG
    public const string DllPath = "../../../../../src/ModInstaller.Native/bin/Release/net9.0/win-x64/native/ModInstaller.Native.dll";
#else
    public const string DllPath = "../../../../../src/ModInstaller.Native/bin/Release/net9.0/win-x64/native/ModInstaller.Native.dll";
#endif


    [LibraryImport(DllPath), UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe partial void* common_alloc(nuint size);
    [LibraryImport(DllPath), UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe partial void common_dealloc(void* ptr);
    [LibraryImport(DllPath), UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe partial int common_alloc_alive_count();

    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        IncludeFields = false,
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin),
        Converters =
        {
            new JsonStringEnumConverter(),
        }
    };
    internal static readonly SourceGenerationContext CustomSourceGenerationContext = new(Options);

    public static unsafe void LibrarySetAllocator() => Allocator.SetCustom(&common_alloc, &common_dealloc);
    public static int LibraryAliveCount() => common_alloc_alive_count();

    public static unsafe SafeStringMallocHandle ToString(string value) => BUTR.NativeAOT.Shared.Utils.Copy(value, true);
    public static unsafe ReadOnlySpan<char> ToSpan(param_string* value) => new SafeStringMallocHandle((char*) value, false).ToSpan();
    public static SafeStringMallocHandle ToJson<T>(T? value) where T : class => BUTR.NativeAOT.Shared.Utils.SerializeJsonCopy(value, (JsonTypeInfo<T>) CustomSourceGenerationContext.GetTypeInfo(typeof(T))!, true);

    public static unsafe T? GetResult<T>(return_value_json* ret) where T : class
    {
        using var result = SafeStructMallocHandle.Create(ret, true);
        var tt = CustomSourceGenerationContext.GetTypeInfo(typeof(T));
        return result.ValueAsJson((JsonTypeInfo<T>) tt!);
    }
    public static unsafe string GetResult(return_value_string* ret)
    {
        using var result = SafeStructMallocHandle.Create(ret, true);
        using var str = result.ValueAsString();
        return str.ToSpan().ToString();
    }
    public static unsafe bool GetResult(return_value_bool* ret)
    {
        using var result = SafeStructMallocHandle.Create(ret, true);
        return result.ValueAsBool();
    }
    public static unsafe int GetResult(return_value_int32* ret)
    {
        using var result = SafeStructMallocHandle.Create(ret, true);
        return result.ValueAsInt32();
    }
    public static unsafe uint GetResult(return_value_uint32* ret)
    {
        using var result = SafeStructMallocHandle.Create(ret, true);
        return result.ValueAsUInt32();
    }
    public static unsafe void GetResult(return_value_void* ret)
    {
        using var result = SafeStructMallocHandle.Create(ret, true);
        result.ValueAsVoid();
    }
    public static unsafe void* GetResult(return_value_ptr* ret)
    {
        using var result = SafeStructMallocHandle.Create(ret, true);
        return result.ValueAsPointer();
    }
    public static unsafe void GetResult(return_value_async* ret)
    {
        using var result = SafeStructMallocHandle.Create(ret, true);
        result.ValueAsAsync();
    }

    public static unsafe void GetResult(return_value_void* ret, TaskCompletionSource tcs)
    {
        using var result = SafeStructMallocHandle.Create(ret, true);
        result.SetAsVoid(tcs);
    }
    public static unsafe void GetResult<T>(return_value_json* ret, TaskCompletionSource<T?> tcs) where T : class
    {
        using var result = SafeStructMallocHandle.Create(ret, true);
        result.SetAsJson(tcs, (JsonTypeInfo<T>) CustomSourceGenerationContext.GetTypeInfo(typeof(T))!);
    }
    public static unsafe void GetResult(return_value_string* ret, TaskCompletionSource<string?> tcs)
    {
        using var result = SafeStructMallocHandle.Create(ret, true);
        result.SetAsString(tcs);
    }
    public static unsafe void GetResult(return_value_bool* ret, TaskCompletionSource<bool> tcs)
    {
        using var result = SafeStructMallocHandle.Create(ret, true);
        result.SetAsBool(tcs);
    }
    public static unsafe void GetResult(return_value_int32* ret, TaskCompletionSource<int> tcs)
    {
        using var result = SafeStructMallocHandle.Create(ret, true);
        result.SetAsInt32(tcs);
    }
    public static unsafe void GetResult(return_value_uint32* ret, TaskCompletionSource<uint> tcs)
    {
        using var result = SafeStructMallocHandle.Create(ret, true);
        result.SetAsUInt32(tcs);
    }
    public static unsafe void GetResult(return_value_ptr* ret, TaskCompletionSource<IntPtr> tcs)
    {
        using var result = SafeStructMallocHandle.Create(ret, true);
        result.SetAsPointer(tcs);
    }
    public static unsafe void GetResult(return_value_async* ret, TaskCompletionSource tcs)
    {
        using var result = SafeStructMallocHandle.Create(ret, true);
        result.SetAsAsync(tcs);
    }
}