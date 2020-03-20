typedef uint32_t Il2CppMethodSlot;
const int ipv6AddressSize = 16;
typedef int32_t il2cpp_hresult_t;
enum Il2CppTypeEnum {
 IL2CPP_TYPE_END = 0x00,
 IL2CPP_TYPE_VOID = 0x01,
 IL2CPP_TYPE_BOOLEAN = 0x02,
 IL2CPP_TYPE_CHAR = 0x03,
 IL2CPP_TYPE_I1 = 0x04,
 IL2CPP_TYPE_U1 = 0x05,
 IL2CPP_TYPE_I2 = 0x06,
 IL2CPP_TYPE_U2 = 0x07,
 IL2CPP_TYPE_I4 = 0x08,
 IL2CPP_TYPE_U4 = 0x09,
 IL2CPP_TYPE_I8 = 0x0a,
 IL2CPP_TYPE_U8 = 0x0b,
 IL2CPP_TYPE_R4 = 0x0c,
 IL2CPP_TYPE_R8 = 0x0d,
 IL2CPP_TYPE_STRING = 0x0e,
 IL2CPP_TYPE_PTR = 0x0f,
 IL2CPP_TYPE_BYREF = 0x10,
 IL2CPP_TYPE_VALUETYPE = 0x11,
 IL2CPP_TYPE_CLASS = 0x12,
 IL2CPP_TYPE_VAR = 0x13,
 IL2CPP_TYPE_ARRAY = 0x14,
 IL2CPP_TYPE_GENERICINST= 0x15,
 IL2CPP_TYPE_TYPEDBYREF = 0x16,
 IL2CPP_TYPE_I = 0x18,
 IL2CPP_TYPE_U = 0x19,
 IL2CPP_TYPE_FNPTR = 0x1b,
 IL2CPP_TYPE_OBJECT = 0x1c,
 IL2CPP_TYPE_SZARRAY = 0x1d,
 IL2CPP_TYPE_MVAR = 0x1e,
 IL2CPP_TYPE_CMOD_REQD = 0x1f,
 IL2CPP_TYPE_CMOD_OPT = 0x20,
 IL2CPP_TYPE_INTERNAL = 0x21,
 IL2CPP_TYPE_MODIFIER = 0x40,
 IL2CPP_TYPE_SENTINEL = 0x41,
 IL2CPP_TYPE_PINNED = 0x45,
 IL2CPP_TYPE_ENUM = 0x55
};
typedef int32_t TypeIndex;
typedef int32_t TypeDefinitionIndex;
typedef int32_t FieldIndex;
typedef int32_t DefaultValueIndex;
typedef int32_t DefaultValueDataIndex;
typedef int32_t CustomAttributeIndex;
typedef int32_t ParameterIndex;
typedef int32_t MethodIndex;
typedef int32_t GenericMethodIndex;
typedef int32_t PropertyIndex;
typedef int32_t EventIndex;
typedef int32_t GenericContainerIndex;
typedef int32_t GenericParameterIndex;
typedef int16_t GenericParameterConstraintIndex;
typedef int32_t NestedTypeIndex;
typedef int32_t InterfacesIndex;
typedef int32_t VTableIndex;
typedef int32_t InterfaceOffsetIndex;
typedef int32_t RGCTXIndex;
typedef int32_t StringIndex;
typedef int32_t StringLiteralIndex;
typedef int32_t GenericInstIndex;
typedef int32_t ImageIndex;
typedef int32_t AssemblyIndex;
typedef int32_t GuidIndex;
const TypeIndex kTypeIndexInvalid = -1;
const TypeDefinitionIndex kTypeDefinitionIndexInvalid = -1;
const DefaultValueDataIndex kDefaultValueIndexNull = -1;
const EventIndex kEventIndexInvalid = -1;
const FieldIndex kFieldIndexInvalid = -1;
const MethodIndex kMethodIndexInvalid = -1;
const PropertyIndex kPropertyIndexInvalid = -1;
const GenericContainerIndex kGenericContainerIndexInvalid = -1;
const GenericParameterIndex kGenericParameterIndexInvalid = -1;
const RGCTXIndex kRGCTXIndexInvalid = -1;
const StringLiteralIndex kStringLiteralIndexInvalid = -1;
const GuidIndex kGuidIndexInvalid = -1;
typedef uint32_t EncodedMethodIndex;
enum Il2CppMetadataUsage
{
 kIl2CppMetadataUsageInvalid,
 kIl2CppMetadataUsageTypeInfo,
 kIl2CppMetadataUsageIl2CppType,
 kIl2CppMetadataUsageMethodDef,
 kIl2CppMetadataUsageFieldInfo,
 kIl2CppMetadataUsageStringLiteral,
 kIl2CppMetadataUsageMethodRef,
};
static inline Il2CppMetadataUsage GetEncodedIndexType (EncodedMethodIndex index)
{
 return (Il2CppMetadataUsage)((index & 0xE0000000) >> 29);
}
static inline uint32_t GetDecodedMethodIndex (EncodedMethodIndex index)
{
 return index & 0x1FFFFFFFU;
}
struct Il2CppImage;
struct Il2CppType;
struct Il2CppTypeDefinitionMetadata;
union Il2CppRGCTXDefinitionData
{
 int32_t rgctxDataDummy;
 MethodIndex methodIndex;
 TypeIndex typeIndex;
};
enum Il2CppRGCTXDataType
{
 IL2CPP_RGCTX_DATA_INVALID,
 IL2CPP_RGCTX_DATA_TYPE,
 IL2CPP_RGCTX_DATA_CLASS,
 IL2CPP_RGCTX_DATA_METHOD
};
struct Il2CppRGCTXDefinition
{
 Il2CppRGCTXDataType type;
 Il2CppRGCTXDefinitionData data;
};
struct Il2CppInterfaceOffsetPair
{
 TypeIndex interfaceTypeIndex;
 int32_t offset;
};
struct Il2CppTypeDefinition
{
 StringIndex nameIndex;
 StringIndex namespaceIndex;
 CustomAttributeIndex customAttributeIndex;
 TypeIndex byvalTypeIndex;
 TypeIndex byrefTypeIndex;
 TypeIndex declaringTypeIndex;
 TypeIndex parentIndex;
 TypeIndex elementTypeIndex;
 RGCTXIndex rgctxStartIndex;
 int32_t rgctxCount;
 GenericContainerIndex genericContainerIndex;
 MethodIndex delegateWrapperFromManagedToNativeIndex;
 int32_t marshalingFunctionsIndex;
 int32_t ccwFunctionIndex;
 GuidIndex guidIndex;
 uint32_t flags;
 FieldIndex fieldStart;
 MethodIndex methodStart;
 EventIndex eventStart;
 PropertyIndex propertyStart;
 NestedTypeIndex nestedTypesStart;
 InterfacesIndex interfacesStart;
 VTableIndex vtableStart;
 InterfacesIndex interfaceOffsetsStart;
 uint16_t method_count;
 uint16_t property_count;
 uint16_t field_count;
 uint16_t event_count;
 uint16_t nested_type_count;
 uint16_t vtable_count;
 uint16_t interfaces_count;
 uint16_t interface_offsets_count;
 uint32_t bitfield;
 uint32_t token;
};
struct Il2CppFieldDefinition
{
 StringIndex nameIndex;
 TypeIndex typeIndex;
 CustomAttributeIndex customAttributeIndex;
 uint32_t token;
};
struct Il2CppFieldDefaultValue
{
 FieldIndex fieldIndex;
 TypeIndex typeIndex;
 DefaultValueDataIndex dataIndex;
};
struct Il2CppFieldMarshaledSize
{
 FieldIndex fieldIndex;
 TypeIndex typeIndex;
 int32_t size;
};
struct Il2CppFieldRef
{
 TypeIndex typeIndex;
 FieldIndex fieldIndex;
};
struct Il2CppParameterDefinition
{
 StringIndex nameIndex;
 uint32_t token;
 CustomAttributeIndex customAttributeIndex;
 TypeIndex typeIndex;
};
struct Il2CppParameterDefaultValue
{
 ParameterIndex parameterIndex;
 TypeIndex typeIndex;
 DefaultValueDataIndex dataIndex;
};
struct Il2CppMethodDefinition
{
 StringIndex nameIndex;
 TypeDefinitionIndex declaringType;
 TypeIndex returnType;
 ParameterIndex parameterStart;
 CustomAttributeIndex customAttributeIndex;
 GenericContainerIndex genericContainerIndex;
 MethodIndex methodIndex;
 MethodIndex invokerIndex;
 MethodIndex delegateWrapperIndex;
 RGCTXIndex rgctxStartIndex;
 int32_t rgctxCount;
 uint32_t token;
 uint16_t flags;
 uint16_t iflags;
 uint16_t slot;
 uint16_t parameterCount;
};
struct Il2CppEventDefinition
{
 StringIndex nameIndex;
 TypeIndex typeIndex;
 MethodIndex add;
 MethodIndex remove;
 MethodIndex raise;
 CustomAttributeIndex customAttributeIndex;
 uint32_t token;
};
struct Il2CppPropertyDefinition
{
 StringIndex nameIndex;
 MethodIndex get;
 MethodIndex set;
 uint32_t attrs;
 CustomAttributeIndex customAttributeIndex;
 uint32_t token;
};
struct Il2CppMethodSpec
{
 MethodIndex methodDefinitionIndex;
 GenericInstIndex classIndexIndex;
 GenericInstIndex methodIndexIndex;
};
struct Il2CppStringLiteral
{
 uint32_t length;
 StringLiteralIndex dataIndex;
};
struct Il2CppGenericMethodIndices
{
 MethodIndex methodIndex;
 MethodIndex invokerIndex;
};
struct Il2CppGenericMethodFunctionsDefinitions
{
 GenericMethodIndex genericMethodIndex;
 Il2CppGenericMethodIndices indices;
};
struct Il2CppAssemblyName
{
 StringIndex nameIndex;
 StringIndex cultureIndex;
 StringIndex hashValueIndex;
 StringIndex publicKeyIndex;
 uint32_t hash_alg;
 int32_t hash_len;
 uint32_t flags;
 int32_t major;
 int32_t minor;
 int32_t build;
 int32_t revision;
 uint8_t publicKeyToken[8];
};
struct Il2CppImageDefinition
{
 StringIndex nameIndex;
 AssemblyIndex assemblyIndex;
 TypeDefinitionIndex typeStart;
 uint32_t typeCount;
 MethodIndex entryPointIndex;
 uint32_t token;
};
struct Il2CppAssembly
{
 ImageIndex imageIndex;
 CustomAttributeIndex customAttributeIndex;
 int32_t referencedAssemblyStart;
 int32_t referencedAssemblyCount;
 Il2CppAssemblyName aname;
};
struct Il2CppMetadataUsageList
{
 uint32_t start;
 uint32_t count;
};
struct Il2CppMetadataUsagePair
{
 uint32_t destinationIndex;
 uint32_t encodedSourceIndex;
};
struct Il2CppCustomAttributeTypeRange
{
 int32_t start;
 int32_t count;
};
#pragma pack(push, p1,4)
struct Il2CppGlobalMetadataHeader
{
 int32_t sanity;
 int32_t version;
 int32_t stringLiteralOffset;
 int32_t stringLiteralCount;
 int32_t stringLiteralDataOffset;
 int32_t stringLiteralDataCount;
 int32_t stringOffset;
 int32_t stringCount;
 int32_t eventsOffset;
 int32_t eventsCount;
 int32_t propertiesOffset;
 int32_t propertiesCount;
 int32_t methodsOffset;
 int32_t methodsCount;
 int32_t parameterDefaultValuesOffset;
 int32_t parameterDefaultValuesCount;
 int32_t fieldDefaultValuesOffset;
 int32_t fieldDefaultValuesCount;
 int32_t fieldAndParameterDefaultValueDataOffset;
 int32_t fieldAndParameterDefaultValueDataCount;
 int32_t fieldMarshaledSizesOffset;
 int32_t fieldMarshaledSizesCount;
 int32_t parametersOffset;
 int32_t parametersCount;
 int32_t fieldsOffset;
 int32_t fieldsCount;
 int32_t genericParametersOffset;
 int32_t genericParametersCount;
 int32_t genericParameterConstraintsOffset;
 int32_t genericParameterConstraintsCount;
 int32_t genericContainersOffset;
 int32_t genericContainersCount;
 int32_t nestedTypesOffset;
 int32_t nestedTypesCount;
 int32_t interfacesOffset;
 int32_t interfacesCount;
 int32_t vtableMethodsOffset;
 int32_t vtableMethodsCount;
 int32_t interfaceOffsetsOffset;
 int32_t interfaceOffsetsCount;
 int32_t typeDefinitionsOffset;
 int32_t typeDefinitionsCount;
 int32_t rgctxEntriesOffset;
 int32_t rgctxEntriesCount;
 int32_t imagesOffset;
 int32_t imagesCount;
 int32_t assembliesOffset;
 int32_t assembliesCount;
 int32_t metadataUsageListsOffset;
 int32_t metadataUsageListsCount;
 int32_t metadataUsagePairsOffset;
 int32_t metadataUsagePairsCount;
 int32_t fieldRefsOffset;
 int32_t fieldRefsCount;
 int32_t referencedAssembliesOffset;
 int32_t referencedAssembliesCount;
 int32_t attributesInfoOffset;
 int32_t attributesInfoCount;
 int32_t attributeTypesOffset;
 int32_t attributeTypesCount;
};
#pragma pack(pop, p1)
struct Il2CppClass;
struct MethodInfo;
struct Il2CppType;
struct Il2CppArrayType {
 const Il2CppType* etype;
 uint8_t rank;
 uint8_t numsizes;
 uint8_t numlobounds;
 int *sizes;
 int *lobounds;
};
struct Il2CppGenericInst {
 uint32_t type_argc;
 const Il2CppType **type_argv;
};
struct Il2CppGenericContext {
 const Il2CppGenericInst *class_inst;
 const Il2CppGenericInst *method_inst;
};
struct Il2CppGenericParameter
{
 GenericContainerIndex ownerIndex;
 StringIndex nameIndex;
 GenericParameterConstraintIndex constraintsStart;
 int16_t constraintsCount;
 uint16_t num;
 uint16_t flags;
};
struct Il2CppGenericContainer
{
 int32_t ownerIndex;
 int32_t type_argc;
 int32_t is_method;
 GenericParameterIndex genericParameterStart;
};
struct Il2CppGenericClass
{
 TypeDefinitionIndex typeDefinitionIndex;
 Il2CppGenericContext context;
 Il2CppClass *cached_class;
};
struct Il2CppGenericMethod
{
 const MethodInfo* methodDefinition;
 Il2CppGenericContext context;
};
struct Il2CppType {
 union {
  void* dummy;
  TypeDefinitionIndex klassIndex;
  const Il2CppType *type;
  Il2CppArrayType *array;
  GenericParameterIndex genericParameterIndex;
  Il2CppGenericClass *generic_class;
 } data;
 unsigned int attrs : 16;
 Il2CppTypeEnum type : 8;
 unsigned int num_mods : 6;
 unsigned int byref : 1;
 unsigned int pinned : 1;
};
typedef enum {
 IL2CPP_CALL_DEFAULT,
 IL2CPP_CALL_C,
 IL2CPP_CALL_STDCALL,
 IL2CPP_CALL_THISCALL,
 IL2CPP_CALL_FASTCALL,
 IL2CPP_CALL_VARARG
} Il2CppCallConvention;
enum Il2CppCharSet
{
 CHARSET_ANSI,
 CHARSET_UNICODE
};
struct PInvokeArguments
{
 const char* moduleName;
 const char* entryPoint;
 Il2CppCallConvention callingConvention;
 Il2CppCharSet charSet;
 int parameterSize;
 bool isNoMangle;
};
struct Il2CppClass;
struct Il2CppType;
struct EventInfo;
struct MethodInfo;
struct FieldInfo;
struct PropertyInfo;
struct Il2CppAssembly;
struct Il2CppArray;
struct Il2CppDelegate;
struct Il2CppDomain;
struct Il2CppImage;
struct Il2CppException;
struct Il2CppProfiler;
struct Il2CppObject;
struct Il2CppReflectionMethod;
struct Il2CppReflectionType;
struct Il2CppString;
struct Il2CppAsyncResult;
enum Il2CppProfileFlags {
 IL2CPP_PROFILE_NONE = 0,
 IL2CPP_PROFILE_APPDOMAIN_EVENTS = 1 << 0,
 IL2CPP_PROFILE_ASSEMBLY_EVENTS = 1 << 1,
 IL2CPP_PROFILE_MODULE_EVENTS = 1 << 2,
 IL2CPP_PROFILE_CLASS_EVENTS = 1 << 3,
 IL2CPP_PROFILE_JIT_COMPILATION = 1 << 4,
 IL2CPP_PROFILE_INLINING = 1 << 5,
 IL2CPP_PROFILE_EXCEPTIONS = 1 << 6,
 IL2CPP_PROFILE_ALLOCATIONS = 1 << 7,
 IL2CPP_PROFILE_GC = 1 << 8,
 IL2CPP_PROFILE_THREADS = 1 << 9,
 IL2CPP_PROFILE_REMOTING = 1 << 10,
 IL2CPP_PROFILE_TRANSITIONS = 1 << 11,
 IL2CPP_PROFILE_ENTER_LEAVE = 1 << 12,
 IL2CPP_PROFILE_COVERAGE = 1 << 13,
 IL2CPP_PROFILE_INS_COVERAGE = 1 << 14,
 IL2CPP_PROFILE_STATISTICAL = 1 << 15,
 IL2CPP_PROFILE_METHOD_EVENTS = 1 << 16,
 IL2CPP_PROFILE_MONITOR_EVENTS = 1 << 17,
 IL2CPP_PROFILE_IOMAP_EVENTS = 1 << 18,
 IL2CPP_PROFILE_GC_MOVES = 1 << 19
};
enum Il2CppGCEvent {
 IL2CPP_GC_EVENT_START,
 IL2CPP_GC_EVENT_MARK_START,
 IL2CPP_GC_EVENT_MARK_END,
 IL2CPP_GC_EVENT_RECLAIM_START,
 IL2CPP_GC_EVENT_RECLAIM_END,
 IL2CPP_GC_EVENT_END,
 IL2CPP_GC_EVENT_PRE_STOP_WORLD,
 IL2CPP_GC_EVENT_POST_STOP_WORLD,
 IL2CPP_GC_EVENT_PRE_START_WORLD,
 IL2CPP_GC_EVENT_POST_START_WORLD
};
enum Il2CppStat {
 IL2CPP_STAT_NEW_OBJECT_COUNT,
 IL2CPP_STAT_INITIALIZED_CLASS_COUNT,
 IL2CPP_STAT_METHOD_COUNT,
 IL2CPP_STAT_CLASS_STATIC_DATA_SIZE,
 IL2CPP_STAT_GENERIC_INSTANCE_COUNT,
 IL2CPP_STAT_GENERIC_CLASS_COUNT,
 IL2CPP_STAT_INFLATED_METHOD_COUNT,
 IL2CPP_STAT_INFLATED_TYPE_COUNT,
};
enum StackFrameType
{
 FRAME_TYPE_MANAGED = 0,
 FRAME_TYPE_DEBUGGER_INVOKE = 1,
 FRAME_TYPE_MANAGED_TO_NATIVE = 2,
 FRAME_TYPE_SENTINEL = 3
};
enum Il2CppRuntimeUnhandledExceptionPolicy {
 IL2CPP_UNHANDLED_POLICY_LEGACY,
 IL2CPP_UNHANDLED_POLICY_CURRENT
};
struct Il2CppStackFrameInfo
{
 const MethodInfo *method;
};
typedef struct {
 void* (*malloc_func)(size_t size);
 void* (*aligned_malloc_func)(size_t size, size_t alignment);
 void (*free_func)(void *ptr);
 void (*aligned_free_func)(void *ptr);
 void* (*calloc_func)(size_t nmemb, size_t size);
 void* (*realloc_func)(void *ptr, size_t size);
 void* (*aligned_realloc_func)(void *ptr, size_t size, size_t alignment);
} Il2CppMemoryCallbacks;
typedef void (*il2cpp_register_object_callback)(Il2CppObject** arr, int size, void* userdata);
typedef void (*il2cpp_WorldChangedCallback)();
typedef void (*Il2CppFrameWalkFunc) (const Il2CppStackFrameInfo *info, void *user_data);
typedef void (*Il2CppProfileFunc) (Il2CppProfiler* prof);
typedef void (*Il2CppProfileMethodFunc) (Il2CppProfiler* prof, const MethodInfo *method);
typedef void (*Il2CppProfileAllocFunc) (Il2CppProfiler* prof, Il2CppObject *obj, Il2CppClass *klass);
typedef void (*Il2CppProfileGCFunc) (Il2CppProfiler* prof, Il2CppGCEvent event, int generation);
typedef void (*Il2CppProfileGCResizeFunc) (Il2CppProfiler* prof, int64_t new_size);
typedef const char* (*Il2CppSetFindPlugInCallback)(const char*);
struct Il2CppManagedMemorySnapshot;
typedef void (*Il2CppMethodPointer)();
typedef int32_t il2cpp_array_size_t;
typedef uint16_t Il2CppChar;
struct Il2CppClass;
struct Il2CppGuid;
struct Il2CppImage;
struct Il2CppAssembly;
struct Il2CppAppDomain;
struct Il2CppDelegate;
struct Il2CppAppContext;
struct Il2CppNameToTypeDefinitionIndexHashTable;
enum Il2CppTypeNameFormat {
 IL2CPP_TYPE_NAME_FORMAT_IL,
 IL2CPP_TYPE_NAME_FORMAT_REFLECTION,
 IL2CPP_TYPE_NAME_FORMAT_FULL_NAME,
 IL2CPP_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED
};
extern bool g_il2cpp_is_fully_initialized;
typedef struct {
 Il2CppImage *corlib;
 Il2CppClass *object_class;
 Il2CppClass *byte_class;
 Il2CppClass *void_class;
 Il2CppClass *boolean_class;
 Il2CppClass *sbyte_class;
 Il2CppClass *int16_class;
 Il2CppClass *uint16_class;
 Il2CppClass *int32_class;
 Il2CppClass *uint32_class;
 Il2CppClass *int_class;
 Il2CppClass *uint_class;
 Il2CppClass *int64_class;
 Il2CppClass *uint64_class;
 Il2CppClass *single_class;
 Il2CppClass *double_class;
 Il2CppClass *char_class;
 Il2CppClass *string_class;
 Il2CppClass *enum_class;
 Il2CppClass *array_class;
 Il2CppClass *delegate_class;
 Il2CppClass *multicastdelegate_class;
 Il2CppClass *asyncresult_class;
 Il2CppClass *manualresetevent_class;
 Il2CppClass *typehandle_class;
 Il2CppClass *fieldhandle_class;
 Il2CppClass *methodhandle_class;
 Il2CppClass *systemtype_class;
 Il2CppClass *monotype_class;
 Il2CppClass *exception_class;
 Il2CppClass *threadabortexception_class;
 Il2CppClass *thread_class;
 Il2CppClass *appdomain_class;
 Il2CppClass *appdomain_setup_class;
 Il2CppClass *field_info_class;
 Il2CppClass *method_info_class;
 Il2CppClass *property_info_class;
 Il2CppClass *event_info_class;
 Il2CppClass *mono_event_info_class;
 Il2CppClass *stringbuilder_class;
 Il2CppClass *stack_frame_class;
 Il2CppClass *stack_trace_class;
 Il2CppClass *marshal_class;
 Il2CppClass *typed_reference_class;
 Il2CppClass *marshalbyrefobject_class;
 Il2CppClass *generic_ilist_class;
 Il2CppClass *generic_icollection_class;
 Il2CppClass *generic_ienumerable_class;
 Il2CppClass *generic_nullable_class;
 Il2CppClass *il2cpp_com_object_class;
 Il2CppClass *customattribute_data_class;
 Il2CppClass *version;
 Il2CppClass *culture_info;
 Il2CppClass *async_call_class;
 Il2CppClass *assembly_class;
 Il2CppClass *assembly_name_class;
 Il2CppClass *enum_info_class;
 Il2CppClass *mono_field_class;
 Il2CppClass *mono_method_class;
 Il2CppClass *mono_method_info_class;
 Il2CppClass *mono_property_info_class;
 Il2CppClass *parameter_info_class;
 Il2CppClass *module_class;
 Il2CppClass *pointer_class;
 Il2CppClass *system_exception_class;
 Il2CppClass *argument_exception_class;
 Il2CppClass *wait_handle_class;
 Il2CppClass *safe_handle_class;
 Il2CppClass *sort_key_class;
 Il2CppClass *dbnull_class;
 Il2CppClass *error_wrapper_class;
 Il2CppClass *missing_class;
 Il2CppClass *value_type_class;
} Il2CppDefaults;
extern Il2CppDefaults il2cpp_defaults;
struct Il2CppClass;
struct MethodInfo;
struct FieldInfo;
struct Il2CppObject;
struct MemberInfo;
struct CustomAttributesCache
{
 int count;
 Il2CppObject** attributes;
};
struct CustomAttributeTypeCache
{
 int count;
 Il2CppClass** attributeTypes;
};
typedef void (*CustomAttributesCacheGenerator)(CustomAttributesCache*);
const int THREAD_STATIC_FIELD_OFFSET = -1;
struct FieldInfo
{
 const char* name;
 const Il2CppType* type;
 Il2CppClass *parent;
 int32_t offset;
 CustomAttributeIndex customAttributeIndex;
 uint32_t token;
};
struct PropertyInfo
{
 Il2CppClass *parent;
 const char *name;
 const MethodInfo *get;
 const MethodInfo *set;
 uint32_t attrs;
 CustomAttributeIndex customAttributeIndex;
 uint32_t token;
};
struct EventInfo
{
 const char* name;
 const Il2CppType* eventType;
 Il2CppClass* parent;
 const MethodInfo* add;
 const MethodInfo* remove;
 const MethodInfo* raise;
 CustomAttributeIndex customAttributeIndex;
 uint32_t token;
};
struct ParameterInfo
{
 const char* name;
 int32_t position;
 uint32_t token;
 CustomAttributeIndex customAttributeIndex;
 const Il2CppType* parameter_type;
};
typedef void* (*InvokerMethod)(const MethodInfo*, void*, void**);
union Il2CppRGCTXData
{
 void* rgctxDataDummy;
 const MethodInfo* method;
 const Il2CppType* type;
 Il2CppClass* klass;
};
struct MethodInfo
{
 Il2CppMethodPointer method;
 InvokerMethod invoker_method;
 const char* name;
 Il2CppClass *declaring_type;
 const Il2CppType *return_type;
 const ParameterInfo* parameters;
 union
 {
  const Il2CppRGCTXData* rgctx_data;
  const Il2CppMethodDefinition* methodDefinition;
 };
 union
 {
  const Il2CppGenericMethod* genericMethod;
  const Il2CppGenericContainer* genericContainer;
 };
 CustomAttributeIndex customAttributeIndex;
 uint32_t token;
 uint16_t flags;
 uint16_t iflags;
 uint16_t slot;
 uint8_t parameters_count;
 uint8_t is_generic : 1;
 uint8_t is_inflated : 1;
};
struct Il2CppRuntimeInterfaceOffsetPair
{
 Il2CppClass* interfaceType;
 int32_t offset;
};
struct Il2CppClass_0
{
 const Il2CppImage* image;
 void* gc_desc;
 const char* name;
 const char* namespaze;
 const Il2CppType* byval_arg;
 const Il2CppType* this_arg;
 Il2CppClass* element_class;
 Il2CppClass* castClass;
 Il2CppClass* declaringType;
 Il2CppClass* parent;
 Il2CppGenericClass *generic_class;
 const Il2CppTypeDefinition* typeDefinition;
 FieldInfo* fields;
 const EventInfo* events;
 const PropertyInfo* properties;
 const MethodInfo** methods;
 Il2CppClass** nestedTypes;
 Il2CppClass** implementedInterfaces;
};
struct Il2CppClass_1
{
 Il2CppClass** typeHierarchy;
 uint32_t cctor_started;
 uint32_t cctor_finished;
#ifdef IS_32BIT
 uint32_t __padding;
#endif
 uint64_t cctor_thread;
 GenericContainerIndex genericContainerIndex;
 CustomAttributeIndex customAttributeIndex;
 uint32_t instance_size;
 uint32_t actualSize;
 uint32_t element_size;
 int32_t native_size;
 uint32_t static_fields_size;
 uint32_t thread_static_fields_size;
 int32_t thread_static_fields_offset;
 uint32_t flags;
 uint32_t token;
 uint16_t method_count;
 uint16_t property_count;
 uint16_t field_count;
 uint16_t event_count;
 uint16_t nested_type_count;
 uint16_t vtable_count;
 uint16_t interfaces_count;
 uint16_t interface_offsets_count;
 uint8_t typeHierarchyDepth;
 uint8_t rank;
 uint8_t minimumAlignment;
 uint8_t packingSize;
 uint8_t valuetype : 1;
 uint8_t initialized : 1;
 uint8_t enumtype : 1;
 uint8_t is_generic : 1;
 uint8_t has_references : 1;
 uint8_t init_pending : 1;
 uint8_t size_inited : 1;
 uint8_t has_finalize : 1;
 uint8_t has_cctor : 1;
 uint8_t is_blittable : 1;
 uint8_t is_import : 1;
};
struct Il2CppClass {
 struct Il2CppClass_0 _0;
 const MethodInfo** vtable;
 Il2CppRuntimeInterfaceOffsetPair* interfaceOffsets;
 void* static_fields;
 const Il2CppRGCTXData* rgctx_data;
 struct Il2CppClass_1 _1;
};
struct Il2CppTypeDefinitionSizes
{
 uint32_t instance_size;
 int32_t native_size;
 uint32_t static_fields_size;
 uint32_t thread_static_fields_size;
};
struct Il2CppDomain
{
 Il2CppAppDomain* domain;
 Il2CppObject* setup;
 Il2CppAppContext* default_context;
 const char* friendly_name;
 uint32_t domain_id;
};
struct Il2CppImage
{
 const char* name;
 AssemblyIndex assemblyIndex;
 TypeDefinitionIndex typeStart;
 uint32_t typeCount;
 MethodIndex entryPointIndex;
 Il2CppNameToTypeDefinitionIndexHashTable* nameToClassHashTable;
 uint32_t token;
};
struct Il2CppMarshalingFunctions
{
 Il2CppMethodPointer marshal_to_native_func;
 Il2CppMethodPointer marshal_from_native_func;
 Il2CppMethodPointer marshal_cleanup_func;
};
struct Il2CppCodeGenOptions
{
 bool enablePrimitiveValueTypeGenericSharing;
};
struct Il2CppCodeRegistration
{
 uint32_t methodPointersCount;
 const Il2CppMethodPointer* methodPointers;
 uint32_t delegateWrappersFromNativeToManagedCount;
 const Il2CppMethodPointer** delegateWrappersFromNativeToManaged;
 uint32_t delegateWrappersFromManagedToNativeCount;
 const Il2CppMethodPointer* delegateWrappersFromManagedToNative;
 uint32_t marshalingFunctionsCount;
 const Il2CppMarshalingFunctions* marshalingFunctions;
 uint32_t ccwMarshalingFunctionsCount;
 const Il2CppMethodPointer* ccwMarshalingFunctions;
 uint32_t genericMethodPointersCount;
 const Il2CppMethodPointer* genericMethodPointers;
 uint32_t invokerPointersCount;
 const InvokerMethod* invokerPointers;
 CustomAttributeIndex customAttributeCount;
 const CustomAttributesCacheGenerator* customAttributeGenerators;
 GuidIndex guidCount;
 const Il2CppGuid** guids;
};
struct Il2CppMetadataRegistration
{
 int32_t genericClassesCount;
 Il2CppGenericClass* const * genericClasses;
 int32_t genericInstsCount;
 const Il2CppGenericInst* const * genericInsts;
 int32_t genericMethodTableCount;
 const Il2CppGenericMethodFunctionsDefinitions* genericMethodTable;
 int32_t typesCount;
 const Il2CppType* const * types;
 int32_t methodSpecsCount;
 const Il2CppMethodSpec* methodSpecs;
 FieldIndex fieldOffsetsCount;
 const int32_t* fieldOffsets;
 TypeDefinitionIndex typeDefinitionsSizesCount;
 const Il2CppTypeDefinitionSizes* typeDefinitionsSizes;
 const size_t metadataUsagesCount;
 void** const* metadataUsages;
};
struct Il2CppRuntimeStats
{
 uint64_t new_object_count;
 uint64_t initialized_class_count;
 uint64_t method_count;
 uint64_t class_static_data_size;
 uint64_t generic_instance_count;
 uint64_t generic_class_count;
 uint64_t inflated_method_count;
 uint64_t inflated_type_count;
 bool enabled;
};
extern Il2CppRuntimeStats il2cpp_runtime_stats;
struct Il2CppClass;
struct MethodInfo;
struct PropertyInfo;
struct FieldInfo;
struct EventInfo;
struct Il2CppType;
struct Il2CppAssembly;
struct Il2CppException;
struct Il2CppImage;
struct Il2CppDomain;
struct Il2CppString;
struct Il2CppReflectionMethod;
struct Il2CppAsyncCall;
struct Il2CppIUnknown;
struct MonitorData;
struct Il2CppReflectionAssembly;
struct Il2CppObject
{
 Il2CppClass *klass;
 MonitorData *monitor;
};
typedef int32_t il2cpp_array_lower_bound_t;
struct Il2CppArrayBounds
{
 il2cpp_array_size_t length;
 il2cpp_array_lower_bound_t lower_bound;
};
struct Il2CppArray
{
 Il2CppObject obj;
 Il2CppArrayBounds *bounds;
 il2cpp_array_size_t max_length;
 double vector [32];
};
struct Il2CppString
{
 Il2CppObject object;
 int32_t length;
 uint16_t chars [32];
};
struct Il2CppReflectionType {
 Il2CppObject object;
 const Il2CppType *type;
};
struct Il2CppReflectionMonoType{
 Il2CppReflectionType type;
 Il2CppObject *type_info;
};
struct Il2CppReflectionEvent {
 Il2CppObject object;
 Il2CppObject *cached_add_event;
};
struct Il2CppReflectionMonoEvent
{
 Il2CppReflectionEvent event;
 Il2CppReflectionType* reflectedType;
 const EventInfo* eventInfo;
};
struct Il2CppReflectionMonoEventInfo
{
 Il2CppReflectionType* declaringType;
 Il2CppReflectionType* reflectedType;
 Il2CppString* name;
 Il2CppReflectionMethod* addMethod;
 Il2CppReflectionMethod* removeMethod;
 Il2CppReflectionMethod* raiseMethod;
 uint32_t eventAttributes;
 Il2CppArray* otherMethods;
};
struct Il2CppEnumInfo {
 Il2CppReflectionType *utype;
 Il2CppArray *values;
 Il2CppArray *names;
 void* name_hash;
};
struct Il2CppReflectionField {
 Il2CppObject object;
 Il2CppClass *klass;
 FieldInfo *field;
 Il2CppString *name;
 Il2CppReflectionType *type;
 uint32_t attrs;
};
struct Il2CppReflectionProperty {
 Il2CppObject object;
 Il2CppClass *klass;
 const PropertyInfo *property;
};
struct Il2CppReflectionMethod {
 Il2CppObject object;
 const MethodInfo *method;
 Il2CppString *name;
 Il2CppReflectionType *reftype;
};
struct Il2CppReflectionGenericMethod
{
 Il2CppReflectionMethod base;
};
struct Il2CppMethodInfo {
 Il2CppReflectionType *parent;
 Il2CppReflectionType *ret;
 uint32_t attrs;
 uint32_t implattrs;
 uint32_t callconv;
};
struct Il2CppPropertyInfo {
 Il2CppReflectionType *parent;
 Il2CppString *name;
 Il2CppReflectionMethod *get;
 Il2CppReflectionMethod *set;
 uint32_t attrs;
};
struct Il2CppReflectionParameter{
 Il2CppObject object;
 Il2CppReflectionType *ClassImpl;
 Il2CppObject *DefaultValueImpl;
 Il2CppObject *MemberImpl;
 Il2CppString *NameImpl;
 int32_t PositionImpl;
 uint32_t AttrsImpl;
 Il2CppObject *MarshalAsImpl;
};
struct Il2CppReflectionModule
{
 Il2CppObject obj;
 const Il2CppImage* image;
 Il2CppReflectionAssembly* assembly;
 Il2CppString* fqname;
 Il2CppString* name;
 Il2CppString* scopename;
 bool is_resource;
 uint32_t token;
};
struct Il2CppReflectionAssemblyName
{
 Il2CppObject obj;
 Il2CppString *name;
 Il2CppString *codebase;
 int32_t major, minor, build, revision;
 Il2CppObject *cultureInfo;
 uint32_t flags;
 uint32_t hashalg;
 Il2CppObject *keypair;
 Il2CppArray *publicKey;
 Il2CppArray *keyToken;
 uint32_t versioncompat;
 Il2CppObject *version;
 uint32_t processor_architecture;
};
struct Il2CppReflectionAssembly {
 Il2CppObject object;
 const Il2CppAssembly *assembly;
 Il2CppObject *resolve_event_holder;
 Il2CppObject *evidence;
 Il2CppObject *minimum;
 Il2CppObject *optional;
 Il2CppObject *refuse;
 Il2CppObject *granted;
 Il2CppObject *denied;
 bool from_byte_array;
 Il2CppString *name;
};
struct Il2CppReflectionMarshal {
 Il2CppObject object;
 int32_t count;
 int32_t type;
 int32_t eltype;
 Il2CppString* guid;
 Il2CppString* mcookie;
 Il2CppString* marshaltype;
 Il2CppObject* marshaltyperef;
 int32_t param_num;
 bool has_size;
};
struct Il2CppReflectionPointer
{
 Il2CppObject object;
 void* data;
 Il2CppReflectionType* type;
};
struct Il2CppIntPtr
{
 void* m_value;
};
struct Il2CppException {
 Il2CppObject object;
 Il2CppArray *trace_ips;
 Il2CppException *inner_ex;
 Il2CppString *message;
 Il2CppString *help_link;
 Il2CppString *class_name;
 Il2CppString *stack_trace;
 Il2CppString *remote_stack_trace;
 int32_t remote_stack_index;
 il2cpp_hresult_t hresult;
 Il2CppString *source;
 Il2CppObject *_data;
};
struct Il2CppSystemException {
 Il2CppException base;
};
struct Il2CppArgumentException {
 Il2CppException base;
 Il2CppString *argName;
};
struct Il2CppTypedRef
{
 Il2CppType *type;
 void* value;
 Il2CppClass *klass;
};
struct Il2CppDelegate {
 Il2CppObject object;
 Il2CppMethodPointer method_ptr;
 void* (*invoke_impl)(const MethodInfo*, void*, void**);
 Il2CppObject *target;
 const MethodInfo *method;
 void* delegate_trampoline;
 uint8_t **method_code;
 Il2CppReflectionMethod *method_info;
 Il2CppReflectionMethod *original_method_info;
 Il2CppObject *data;
};
struct Il2CppMarshalByRefObject {
 Il2CppObject obj;
 Il2CppObject *identity;
};
struct Il2CppComObject {
 Il2CppObject obj;
 Il2CppIUnknown *identity;
};
struct Il2CppAppDomain {
 Il2CppMarshalByRefObject mbr;
 Il2CppDomain *data;
};
struct Il2CppStackFrame {
 Il2CppObject obj;
 int32_t il_offset;
 int32_t native_offset;
 Il2CppReflectionMethod *method;
 Il2CppString *filename;
 int32_t line;
 int32_t column;
 Il2CppString *internal_method_name;
};
struct Il2CppDateTimeFormatInfo {
 Il2CppObject obj;
 bool readOnly;
 Il2CppString* AMDesignator;
 Il2CppString* PMDesignator;
 Il2CppString* DateSeparator;
 Il2CppString* TimeSeparator;
 Il2CppString* ShortDatePattern;
 Il2CppString* LongDatePattern;
 Il2CppString* ShortTimePattern;
 Il2CppString* LongTimePattern;
 Il2CppString* MonthDayPattern;
 Il2CppString* YearMonthPattern;
 Il2CppString* FullDateTimePattern;
 Il2CppString* RFC1123Pattern;
 Il2CppString* SortableDateTimePattern;
 Il2CppString* UniversalSortableDateTimePattern;
 uint32_t FirstDayOfWeek;
 Il2CppObject* Calendar;
 uint32_t CalendarWeekRule;
 Il2CppArray* AbbreviatedDayNames;
 Il2CppArray* DayNames;
 Il2CppArray* MonthNames;
 Il2CppArray* AbbreviatedMonthNames;
 Il2CppArray* ShortDatePatterns;
 Il2CppArray* LongDatePatterns;
 Il2CppArray* ShortTimePatterns;
 Il2CppArray* LongTimePatterns;
 Il2CppArray* MonthDayPatterns;
 Il2CppArray* YearMonthPatterns;
 Il2CppArray* shortDayNames;
};
struct Il2CppNumberFormatInfo {
 Il2CppObject obj;
 bool readOnly;
 Il2CppString* decimalFormats;
 Il2CppString* currencyFormats;
 Il2CppString* percentFormats;
 Il2CppString* digitPattern;
 Il2CppString* zeroPattern;
 int32_t currencyDecimalDigits;
 Il2CppString* currencyDecimalSeparator;
 Il2CppString* currencyGroupSeparator;
 Il2CppArray* currencyGroupSizes;
 int32_t currencyNegativePattern;
 int32_t currencyPositivePattern;
 Il2CppString* currencySymbol;
 Il2CppString* naNSymbol;
 Il2CppString* negativeInfinitySymbol;
 Il2CppString* negativeSign;
 uint32_t numberDecimalDigits;
 Il2CppString* numberDecimalSeparator;
 Il2CppString* numberGroupSeparator;
 Il2CppArray* numberGroupSizes;
 int32_t numberNegativePattern;
 int32_t percentDecimalDigits;
 Il2CppString* percentDecimalSeparator;
 Il2CppString* percentGroupSeparator;
 Il2CppArray* percentGroupSizes;
 int32_t percentNegativePattern;
 int32_t percentPositivePattern;
 Il2CppString* percentSymbol;
 Il2CppString* perMilleSymbol;
 Il2CppString* positiveInfinitySymbol;
 Il2CppString* positiveSign;
};
struct Il2CppCultureInfo {
 Il2CppObject obj;
 bool is_read_only;
 int32_t lcid;
 int32_t parent_lcid;
 int32_t specific_lcid;
 int32_t datetime_index;
 int32_t number_index;
 bool use_user_override;
 Il2CppNumberFormatInfo* number_format;
 Il2CppDateTimeFormatInfo* datetime_format;
 Il2CppObject* textinfo;
 Il2CppString* name;
 Il2CppString* displayname;
 Il2CppString* englishname;
 Il2CppString* nativename;
 Il2CppString* iso3lang;
 Il2CppString* iso2lang;
 Il2CppString* icu_name;
 Il2CppString* win3lang;
 Il2CppString* territory;
 Il2CppString* compareinfo;
 const int32_t* calendar_data;
 const void* text_info_data;
};
struct Il2CppWaitHandle {
 Il2CppMarshalByRefObject object;
 void* handle;
 bool disposed;
};
struct Il2CppSafeHandle {
 Il2CppObject base;
 void* handle;
 void* invalid_handle_value;
 int refcount;
 bool owns_handle;
};
struct Il2CppStringBuilder {
 Il2CppObject object;
 int32_t length;
 Il2CppString *str;
 Il2CppString *cached_str;
 int32_t max_capacity;
};
struct Il2CppSocketAddress
{
 Il2CppObject base;
 Il2CppArray* data;
};
struct Il2CppSortKey
{
 Il2CppObject base;
 Il2CppString *str;
 int32_t options;
 Il2CppArray *key;
 int32_t lcid;
};
struct Il2CppErrorWrapper
{
 Il2CppObject base;
 int32_t errorCode;
};
struct Il2CppAsyncResult
{
 Il2CppObject base;
 Il2CppObject *async_state;
 Il2CppWaitHandle *handle;
 Il2CppDelegate *async_delegate;
 void* data;
 Il2CppAsyncCall *object_data;
 bool sync_completed;
 bool completed;
 bool endinvoke_called;
 Il2CppObject *async_callback;
 Il2CppObject *execution_context;
 Il2CppObject *original_context;
};
struct Il2CppAsyncCall
{
 Il2CppObject base;
 void *msg;
 MethodInfo *cb_method;
 Il2CppDelegate *cb_target;
 Il2CppObject *state;
 Il2CppObject *res;
 Il2CppArray *out_args;
 uint64_t wait_event;
};
struct Il2CppExceptionWrapper
{
 Il2CppException* ex;
};
struct Il2CppSocketAsyncResult
{
 Il2CppObject base;
 Il2CppObject *socket;
 Il2CppIntPtr handle;
 Il2CppObject *state;
 Il2CppDelegate *callback;
 Il2CppWaitHandle *wait_handle;
 Il2CppException *delayed_exc;
 Il2CppObject *ep;
 Il2CppArray *buffer;
 int32_t offset;
 int32_t size;
 int32_t socket_flags;
 Il2CppObject *accept_reuse_socket;
 Il2CppArray *addresses;
 int32_t port;
 Il2CppObject *buffers;
 bool reusesocket;
 Il2CppObject *acc_socket;
 int32_t total;
 bool completed_synch;
 bool completed;
 bool blocking;
 int32_t error;
 int32_t operation;
 Il2CppAsyncResult *ares;
};
enum Il2CppResourceLocation
{
 RESOURCE_LOCATION_EMBEDDED = 1,
 RESOURCE_LOCATION_ANOTHER_ASSEMBLY = 2,
 RESOURCE_LOCATION_IN_MANIFEST = 4
};
struct Il2CppManifestResourceInfo
{
 Il2CppObject object;
 Il2CppReflectionAssembly* assembly;
 Il2CppString* filename;
 uint32_t location;
};
struct Il2CppAppContext
{
 Il2CppObject obj;
 int32_t domain_id;
 int32_t context_id;
 void* static_data;
};
