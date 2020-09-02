# IDA-specific implementation
import idaapi

def SetName(addr, name):
	ret = idc.set_name(addr, name, SN_NOWARN | SN_NOCHECK)
	if ret == 0:
		new_name = name + '_' + str(addr)
		ret = idc.set_name(addr, new_name, SN_NOWARN | SN_NOCHECK)

def MakeFunction(start):
  ida_funcs.add_func(start)

def DefineCode(code):
	idc.parse_decls(code)

def SetFunctionType(addr, sig):
  SetType(addr, sig)

def SetType(addr, type):
  ret = idc.SetType(addr, type)
  if ret is None:
    print('SetType(0x%x, %r) failed!' % (addr, type))

def SetComment(addr, text):
  idc.set_cmt(addr, text, 1)

def SetHeaderComment(addr, text):
  SetComment(addr, text)

def CustomInitializer():
	print('Processing Types')

	original_macros = ida_typeinf.get_c_macros()
	ida_typeinf.set_c_macros(original_macros + ";_IDA_=1")
	idc.parse_decls(os.path.join(GetScriptDirectory(), "%TYPE_HEADER_RELATIVE_PATH%"), idc.PT_FILE)
	ida_typeinf.set_c_macros(original_macros)

def GetScriptDirectory():
	return os.path.dirname(os.path.realpath(__file__))
