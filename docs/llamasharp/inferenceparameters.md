Inference Parameters
Different from LLamaModel, when using an exeuctor, InferenceParams is passed to the Infer method instead of constructor. This is because executors only define the ways to run the model, therefore in each run, you can change the settings for this time inference.

InferenceParams
Namespace: LLama.Common

public class InferenceParams
Inheritance Object → InferenceParams

Properties
TokensKeep
number of tokens to keep from initial prompt

public int TokensKeep { get; set; }
Property Value
Int32

MaxTokens
how many new tokens to predict (n_predict), set to -1 to inifinitely generate response until it complete.

public int MaxTokens { get; set; }
Property Value
Int32

LogitBias
logit bias for specific tokens

public Dictionary<int, float> LogitBias { get; set; }
Property Value
Dictionary<Int32, Single>

AntiPrompts
Sequences where the model will stop generating further tokens.

public IEnumerable<string> AntiPrompts { get; set; }
Property Value
IEnumerable<String>

PathSession
path to file for saving/loading model eval state

public string PathSession { get; set; }
Property Value
String

InputSuffix
string to suffix user inputs with

public string InputSuffix { get; set; }
Property Value
String

InputPrefix
string to prefix user inputs with

public string InputPrefix { get; set; }
Property Value
String

TopK
0 or lower to use vocab size

public int TopK { get; set; }
Property Value
Int32

TopP
1.0 = disabled

public float TopP { get; set; }
Property Value
Single

TfsZ
1.0 = disabled

public float TfsZ { get; set; }
Property Value
Single

TypicalP
1.0 = disabled

public float TypicalP { get; set; }
Property Value
Single

Temperature
1.0 = disabled

public float Temperature { get; set; }
Property Value
Single

RepeatPenalty
1.0 = disabled

public float RepeatPenalty { get; set; }
Property Value
Single

RepeatLastTokensCount
last n tokens to penalize (0 = disable penalty, -1 = context size) (repeat_last_n)

public int RepeatLastTokensCount { get; set; }
Property Value
Int32

FrequencyPenalty
frequency penalty coefficient 0.0 = disabled

public float FrequencyPenalty { get; set; }
Property Value
Single

PresencePenalty
presence penalty coefficient 0.0 = disabled

public float PresencePenalty { get; set; }
Property Value
Single

Mirostat
Mirostat uses tokens instead of words. algorithm described in the paper https://arxiv.org/abs/2007.14966. 0 = disabled, 1 = mirostat, 2 = mirostat 2.0

public MiroStateType Mirostat { get; set; }
Property Value
MiroStateType

MirostatTau
target entropy

public float MirostatTau { get; set; }
Property Value
Single

MirostatEta
learning rate

public float MirostatEta { get; set; }
Property Value
Single

PenalizeNL
consider newlines as a repeatable token (penalize_nl)

public bool PenalizeNL { get; set; }
Property Value
Boolean