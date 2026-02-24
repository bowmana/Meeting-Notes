LLamaModel Parameters
When initializing a LLamaModel object, there're three parameters, ModelParams Params, string encoding = "UTF-8", ILLamaLogger? logger = null.

The usage of logger will be further introduced in logger doc. The encoding is the encoding you want to use when dealing with text via this model.

The most improtant of all, is the ModelParams, which is defined as below. We'll explain the parameters step by step in this document.

public class ModelParams
{
    public int ContextSize { get; set; } = 512;
    public int GpuLayerCount { get; set; } = 20;
    public int Seed { get; set; } = 1686349486;
    public bool UseFp16Memory { get; set; } = true;
    public bool UseMemorymap { get; set; } = true;
    public bool UseMemoryLock { get; set; } = false;
    public bool Perplexity { get; set; } = false;
    public string ModelPath { get; set; }
    public string LoraAdapter { get; set; } = string.Empty;
    public string LoraBase { get; set; } = string.Empty;
    public int Threads { get; set; } = Math.Max(Environment.ProcessorCount / 2, 1);
    public int BatchSize { get; set; } = 512;
    public bool ConvertEosToNewLine { get; set; } = false;
}
ModelParams
Namespace: LLama.Common

public class ModelParams
Inheritance Object → ModelParams

Properties
ContextSize
Model context size (n_ctx)

public int ContextSize { get; set; }
Property Value
Int32

GpuLayerCount
Number of layers to run in VRAM / GPU memory (n_gpu_layers)

public int GpuLayerCount { get; set; }
Property Value
Int32

Seed
Seed for the random number generator (seed)

public int Seed { get; set; }
Property Value
Int32

UseFp16Memory
Use f16 instead of f32 for memory kv (memory_f16)

public bool UseFp16Memory { get; set; }
Property Value
Boolean

UseMemorymap
Use mmap for faster loads (use_mmap)

public bool UseMemorymap { get; set; }
Property Value
Boolean

UseMemoryLock
Use mlock to keep model in memory (use_mlock)

public bool UseMemoryLock { get; set; }
Property Value
Boolean

Perplexity
Compute perplexity over the prompt (perplexity)

public bool Perplexity { get; set; }
Property Value
Boolean

ModelPath
Model path (model)

public string ModelPath { get; set; }
Property Value
String

LoraAdapter
lora adapter path (lora_adapter)

public string LoraAdapter { get; set; }
Property Value
String

LoraBase
base model path for the lora adapter (lora_base)

public string LoraBase { get; set; }
Property Value
String

Threads
Number of threads (-1 = autodetect) (n_threads)

public int Threads { get; set; }
Property Value
Int32

BatchSize
batch size for prompt processing (must be >=32 to use BLAS) (n_batch)

public int BatchSize { get; set; }
Property Value
Int32

ConvertEosToNewLine
Whether to convert eos to newline during the inference.

public bool ConvertEosToNewLine { get; set; }
Property Value
Boolean

EmbeddingMode
Whether to use embedding mode. (embedding) Note that if this is set to true, The LLamaModel won't produce text response anymore.

public bool EmbeddingMode { get; set; }
Property Value
Boolean