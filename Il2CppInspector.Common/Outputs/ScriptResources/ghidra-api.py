# Ghidra-specific implementation
from ghidra.app.cmd.function import ApplyFunctionSignatureCmd
from ghidra.app.script import GhidraScriptUtil
from ghidra.app.util.cparser.C import CParserUtils
from ghidra.program.model.symbol import SourceType

def SetName(addr, name):
	createLabel(toAddr(addr), name, True)

def MakeFunction(start, name=None):
	addr = toAddr(start)
	# Don't override existing functions
	fn = getFunctionAt(addr)
	if fn is not None and name is not None:
		# Set existing function name if name available
		fn.setName(name, SourceType.USER_DEFINED)
	elif fn is None:
		# Create new function if none exists
		createFunction(addr, name)
	# Set header comment if name available
	if name is not None:
		setPlateComment(addr, name)

def DefineCode(code):
	# Code declarations are not supported in Ghidra
	# This only affects string literals for metadata version < 19
	# TODO: Replace with creating a DataType for enums
	pass

def SetFunctionType(addr, sig):
	MakeFunction(addr)
	typeSig = CParserUtils.parseSignature(None, currentProgram, sig)
	ApplyFunctionSignatureCmd(toAddr(addr), typeSig, SourceType.USER_DEFINED, False, True).applyTo(currentProgram)

def SetType(addr, type):
	if type.startswith('struct '):
		type = type[7:]
	
	t = getDataTypes(type)[0]
	addr = toAddr(addr)
	removeDataAt(addr)
	createData(addr, t)

def SetComment(addr, text):
	setEOLComment(toAddr(addr), text)

def SetHeaderComment(addr, text):
	setPlateComment(toAddr(addr), text)

def CustomInitializer():
	# Ghidra sets the image base for ELF to 0x100000 for some reason
	# https://github.com/NationalSecurityAgency/ghidra/issues/1020
	if currentProgram.getExecutableFormat().endswith('(ELF)'):
		currentProgram.setImageBase(toAddr(0), True)

def GetScriptDirectory():
	# Ghidra doesn't define __file__ so we have to iterate all the scripts
	return next(iter(filter(lambda x: x.getName() == '%SCRIPTFILENAME%', GhidraScriptUtil.getAllScripts())), None).getParentFile().toString()
