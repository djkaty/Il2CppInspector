typedef void (*methodPointerType)();
typedef int32_t il2cpp_array_size_t;
typedef uint32_t Il2CppMethodSlot;
const int ipv6AddressSize = 16;
typedef enum Il2CppTypeEnum
{
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
} Il2CppTypeEnum;
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
typedef uint32_t EncodedMethodIndex;
static inline bool IsGenericMethodIndex (EncodedMethodIndex index)
{
 return (index & 0x80000000U) != 0;
}
static inline uint32_t GetDecodedMethodIndex (EncodedMethodIndex index)
{
 return index & 0x7FFFFFFFU;
}
typedef struct Il2CppImage Il2CppImage;
typedef struct Il2CppType Il2CppType;
typedef struct Il2CppTypeDefinitionMetadata Il2CppTypeDefinitionMetadata;
typedef union Il2CppRGCTXDefinitionData
{
 int32_t rgctxDataDummy;
 MethodIndex methodIndex;
 TypeIndex typeIndex;
} Il2CppRGCTXDefinitionData;
typedef enum Il2CppRGCTXDataType
{
 IL2CPP_RGCTX_DATA_INVALID,
 IL2CPP_RGCTX_DATA_TYPE,
 IL2CPP_RGCTX_DATA_CLASS,
 IL2CPP_RGCTX_DATA_METHOD
} Il2CppRGCTXDataType;
typedef struct Il2CppRGCTXDefinition
{
 Il2CppRGCTXDataType type;
 Il2CppRGCTXDefinitionData data;
} Il2CppRGCTXDefinition;
typedef struct Il2CppInterfaceOffsetPair
{
 TypeIndex interfaceTypeIndex;
 int32_t offset;
} Il2CppInterfaceOffsetPair;
typedef struct Il2CppTypeDefinition
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
} Il2CppTypeDefinition;
typedef struct Il2CppFieldDefinition
{
 StringIndex nameIndex;
 TypeIndex typeIndex;
 CustomAttributeIndex customAttributeIndex;
} Il2CppFieldDefinition;
typedef struct Il2CppFieldDefaultValue
{
 FieldIndex fieldIndex;
 TypeIndex typeIndex;
 DefaultValueDataIndex dataIndex;
} Il2CppFieldDefaultValue;
typedef struct Il2CppFieldMarshaledSize
{
 FieldIndex fieldIndex;
 TypeIndex typeIndex;
 int32_t size;
} Il2CppFieldMarshaledSize;
typedef struct Il2CppParameterDefinition
{
 StringIndex nameIndex;
 uint32_t token;
 CustomAttributeIndex customAttributeIndex;
 TypeIndex typeIndex;
} Il2CppParameterDefinition;
typedef struct Il2CppParameterDefaultValue
{
 ParameterIndex parameterIndex;
 TypeIndex typeIndex;
 DefaultValueDataIndex dataIndex;
} Il2CppParameterDefaultValue;
typedef struct Il2CppMethodDefinition
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
} Il2CppMethodDefinition;
typedef struct Il2CppEventDefinition
{
 StringIndex nameIndex;
 TypeIndex typeIndex;
 MethodIndex add;
 MethodIndex remove;
 MethodIndex raise;
 CustomAttributeIndex customAttributeIndex;
} Il2CppEventDefinition;
typedef struct Il2CppPropertyDefinition
{
 StringIndex nameIndex;
 MethodIndex get;
 MethodIndex set;
 uint32_t attrs;
 CustomAttributeIndex customAttributeIndex;
} Il2CppPropertyDefinition;
typedef struct Il2CppMethodSpec
{
 MethodIndex methodDefinitionIndex;
 GenericInstIndex classIndexIndex;
 GenericInstIndex methodIndexIndex;
} Il2CppMethodSpec;
typedef struct Il2CppStringLiteral
{
 uint32_t length;
 StringLiteralIndex dataIndex;
} Il2CppStringLiteral;
typedef struct Il2CppGenericMethodIndices
{
 MethodIndex methodIndex;
 MethodIndex invokerIndex;
} Il2CppGenericMethodIndices;
typedef struct Il2CppGenericMethodFunctionsDefinitions
{
 GenericMethodIndex genericMethodIndex;
 Il2CppGenericMethodIndices indices;
} Il2CppGenericMethodFunctionsDefinitions;
const int kPublicKeyByteLength = 8;
typedef struct Il2CppAssemblyName
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
} Il2CppAssemblyName;
typedef struct Il2CppImageDefinition
{
 StringIndex nameIndex;
 AssemblyIndex assemblyIndex;
 TypeDefinitionIndex typeStart;
 uint32_t typeCount;
 MethodIndex entryPointIndex;
} Il2CppImageDefinition;
typedef struct Il2CppAssembly
{
 ImageIndex imageIndex;
 CustomAttributeIndex customAttributeIndex;
 Il2CppAssemblyName aname;
} Il2CppAssembly;
#pragma pack(push, p1,4)
typedef struct Il2CppGlobalMetadataHeader
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
} Il2CppGlobalMetadataHeader;
#pragma pack(pop, p1)
typedef struct Il2CppClass Il2CppClass;
typedef struct MethodInfo MethodInfo;
typedef struct Il2CppType Il2CppType;
typedef struct Il2CppArrayType
{
 const Il2CppType* etype;
 uint8_t rank;
 uint8_t numsizes;
 uint8_t numlobounds;
 int *sizes;
 int *lobounds;
} Il2CppArrayType;
typedef struct Il2CppGenericInst
{
 uint32_t type_argc;
 const Il2CppType **type_argv;
} Il2CppGenericInst;
typedef struct Il2CppGenericContext
{
 const Il2CppGenericInst *class_inst;
 const Il2CppGenericInst *method_inst;
} Il2CppGenericContext;
typedef struct Il2CppGenericParameter
{
 GenericContainerIndex ownerIndex;
 StringIndex nameIndex;
 GenericParameterConstraintIndex constraintsStart;
 int16_t constraintsCount;
 uint16_t num;
 uint16_t flags;
} Il2CppGenericParameter;
typedef struct Il2CppGenericContainer
{
 int32_t ownerIndex;
 int32_t type_argc;
 int32_t is_method;
 GenericParameterIndex genericParameterStart;
} Il2CppGenericContainer;
typedef struct Il2CppGenericClass
{
 TypeDefinitionIndex typeDefinitionIndex;
 Il2CppGenericContext context;
 Il2CppClass *cached_class;
} Il2CppGenericClass;
typedef struct Il2CppGenericMethod
{
 const MethodInfo* methodDefinition;
 Il2CppGenericContext context;
} Il2CppGenericMethod;
typedef struct Il2CppType
{
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
} Il2CppType;
typedef enum {
 IL2CPP_CALL_DEFAULT,
 IL2CPP_CALL_C,
 IL2CPP_CALL_STDCALL,
 IL2CPP_CALL_THISCALL,
 IL2CPP_CALL_FASTCALL,
 IL2CPP_CALL_VARARG
} Il2CppCallConvention;
typedef enum Il2CppCharSet
{
 CHARSET_ANSI,
 CHARSET_UNICODE
} Il2CppCharSet;
typedef struct PInvokeArguments
{
 const char* moduleName;
 const char* entryPoint;
 Il2CppCallConvention callingConvention;
 Il2CppCharSet charSet;
 int parameterSize;
 bool isNoMangle;
} PInvokeArguments;
typedef struct Il2CppClass Il2CppClass;
typedef struct Il2CppImage Il2CppImage;
typedef struct Il2CppAssembly Il2CppAssembly;
typedef struct Il2CppAppDomain Il2CppAppDomain;
typedef struct Il2CppDelegate Il2CppDelegate;
typedef struct Il2CppAppContext Il2CppAppContext;
typedef struct Il2CppNameToTypeDefinitionIndexHashTable Il2CppNameToTypeDefinitionIndexHashTable;
typedef enum Il2CppTypeNameFormat
{
 IL2CPP_TYPE_NAME_FORMAT_IL,
 IL2CPP_TYPE_NAME_FORMAT_REFLECTION,
 IL2CPP_TYPE_NAME_FORMAT_FULL_NAME,
 IL2CPP_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED
} Il2CppTypeNameFormat;
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
} Il2CppDefaults;
extern Il2CppDefaults il2cpp_defaults;
typedef struct Il2CppClass Il2CppClass;
typedef struct MethodInfo MethodInfo;
typedef struct FieldInfo FieldInfo;
typedef struct Il2CppObject Il2CppObject;
typedef struct CustomAttributesCache
{
 int count;
 Il2CppObject** attributes;
} CustomAttributesCache;
typedef void (*CustomAttributesCacheGenerator)(CustomAttributesCache*);
const int THREAD_STATIC_FIELD_OFFSET = -1;
typedef struct FieldInfo
{
 const char* name;
 const Il2CppType* type;
 Il2CppClass *parent;
 int32_t offset;
 CustomAttributeIndex customAttributeIndex;
} FieldInfo;
typedef struct PropertyInfo
{
 Il2CppClass *parent;
 const char *name;
 const MethodInfo *get;
 const MethodInfo *set;
 uint32_t attrs;
 CustomAttributeIndex customAttributeIndex;
} PropertyInfo;
typedef struct EventInfo
{
 const char* name;
 const Il2CppType* eventType;
 Il2CppClass* parent;
 const MethodInfo* add;
 const MethodInfo* remove;
 const MethodInfo* raise;
 CustomAttributeIndex customAttributeIndex;
} EventInfo;
typedef struct ParameterInfo
{
 const char* name;
 int32_t position;
 uint32_t token;
 CustomAttributeIndex customAttributeIndex;
 const Il2CppType* parameter_type;
} ParameterInfo;
typedef void* (*InvokerMethod)(const MethodInfo*, void*, void**);
typedef union Il2CppRGCTXData
{
 void* rgctxDataDummy;
 const MethodInfo* method;
 const Il2CppType* type;
 Il2CppClass* klass;
} Il2CppRGCTXData;
typedef struct MethodInfo
{
 methodPointerType method;
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
} MethodInfo;
typedef struct Il2CppRuntimeInterfaceOffsetPair
{
 Il2CppClass* interfaceType;
 int32_t offset;
} Il2CppRuntimeInterfaceOffsetPair;
typedef struct Il2CppClass
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
 const MethodInfo** vtable;
 Il2CppRuntimeInterfaceOffsetPair* interfaceOffsets;
 void* static_fields;
 const Il2CppRGCTXData* rgctx_data;
 Il2CppClass** typeHierarchy;
 uint32_t cctor_started;
 uint32_t cctor_finished;
 __attribute__((aligned(8))) uint64_t cctor_thread;
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
} Il2CppClass;

typedef struct Il2CppClass_0 {
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
    Il2CppGenericClass * generic_class;
    const Il2CppTypeDefinition* typeDefinition;
    FieldInfo* fields;
    const EventInfo* events;
    const PropertyInfo* properties;
    const MethodInfo** methods;
    Il2CppClass** nestedTypes;
    Il2CppClass** implementedInterfaces;
} Il2CppClass_0;

typedef struct Il2CppClass_1 {
    Il2CppClass** typeHierarchy;
    uint32_t cctor_started;
    uint32_t cctor_finished;
#ifdef IS_32BIT
    uint32_t cctor_thread__padding;
    uint32_t cctor_thread;
    uint32_t cctor_thread__hi;
#else
    __attribute__((aligned(8))) uint64_t cctor_thread;
#endif
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
} Il2CppClass_1;

typedef struct __attribute__((aligned(8))) Il2CppClass_Merged {
    struct Il2CppClass_0 _0;
    const MethodInfo** vtable;
    Il2CppRuntimeInterfaceOffsetPair* interfaceOffsets;
    void* static_fields;
    const Il2CppRGCTXData* rgctx_data;
    struct Il2CppClass_1 _1;
} Il2CppClass_Merged;

typedef struct Il2CppTypeDefinitionSizes
{
 uint32_t instance_size;
 int32_t native_size;
 uint32_t static_fields_size;
 uint32_t thread_static_fields_size;
} Il2CppTypeDefinitionSizes;
typedef struct Il2CppDomain
{
 Il2CppAppDomain* domain;
 Il2CppObject* setup;
 Il2CppAppContext* default_context;
 const char* friendly_name;
 uint32_t domain_id;
} Il2CppDomain;
typedef struct Il2CppImage
{
 const char* name;
 AssemblyIndex assemblyIndex;
 TypeDefinitionIndex typeStart;
 uint32_t typeCount;
 MethodIndex entryPointIndex;
 Il2CppNameToTypeDefinitionIndexHashTable* nameToClassHashTable;
} Il2CppImage;
typedef struct Il2CppMarshalingFunctions
{
 methodPointerType marshal_to_native_func;
 methodPointerType marshal_from_native_func;
 methodPointerType marshal_cleanup_func;
} Il2CppMarshalingFunctions;
typedef struct Il2CppCodeRegistration
{
 uint32_t methodPointersCount;
 const methodPointerType* methodPointers;
 uint32_t delegateWrappersFromNativeToManagedCount;
 const methodPointerType** delegateWrappersFromNativeToManaged;
 uint32_t delegateWrappersFromManagedToNativeCount;
 const methodPointerType* delegateWrappersFromManagedToNative;
 uint32_t marshalingFunctionsCount;
 const Il2CppMarshalingFunctions* marshalingFunctions;
 uint32_t genericMethodPointersCount;
 const methodPointerType* genericMethodPointers;
 uint32_t invokerPointersCount;
 const InvokerMethod* invokerPointers;
 CustomAttributeIndex customAttributeCount;
 const CustomAttributesCacheGenerator* customAttributeGenerators;
} Il2CppCodeRegistration;
typedef struct Il2CppMetadataRegistration
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
 int32_t methodReferencesCount;
 const EncodedMethodIndex* methodReferences;
 FieldIndex fieldOffsetsCount;
 const int32_t* fieldOffsets;
 TypeDefinitionIndex typeDefinitionsSizesCount;
 const Il2CppTypeDefinitionSizes* typeDefinitionsSizes;
} Il2CppMetadataRegistration;
typedef struct Il2CppRuntimeStats
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
} Il2CppRuntimeStats;
extern Il2CppRuntimeStats il2cpp_runtime_stats;

struct MonitorData;
struct Il2CppObject {
    struct Il2CppClass *klass;
    struct MonitorData *monitor;
};
typedef int32_t il2cpp_array_lower_bound_t;
struct Il2CppArrayBounds {
    il2cpp_array_size_t length;
    il2cpp_array_lower_bound_t lower_bound;
};
struct Il2CppArray {
    struct Il2CppObject obj;
    struct Il2CppArrayBounds *bounds;
    il2cpp_array_size_t max_length;
    /* vector must be 8-byte aligned.
       On 64-bit platforms, this happens naturally.
       On 32-bit platforms, sizeof(obj)=8, sizeof(bounds)=4 and sizeof(max_length)=4 so it's also already aligned. */
    void *vector[32];
};
struct Il2CppString {
    struct Il2CppObject object;
    int32_t length;
    uint16_t chars[32];
};
