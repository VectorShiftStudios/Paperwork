using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class CFibreRegStore
{
	public CFibreReg[] mStore = new CFibreReg[128];
}

public class CFibreCallFrame
{
	public int mNextInstruction;
	public int mFramePtr;
	public int mPushedArgCount;

	public CFibreCallFrame(int NextInstruction, int FramePointer, int PushedArgCount)
	{
		mNextInstruction = NextInstruction;
		mFramePtr = FramePointer;
		mPushedArgCount = PushedArgCount;
	}
}

public class CFibreRuntimeException : Exception
{
	public string mFibreInfo;

	public CFibreRuntimeException() { }
	public CFibreRuntimeException(string Message) : base(Message) { }

	public CFibreRuntimeException(string Message, CFibre Fibre) : base(Message)
	{
		mFibreInfo = "(" + Fibre.mScript.GetFileLocationFromOpcode(Fibre.mInstructionPtr - 1) + ")";
	}
}

public class CFibre
{
	public enum ERunResult
	{
		NONE,
		CONTINUE,
		FAILED,
		DONE,
	}

	public delegate CFibreReg BinaryOpDelegate(CFibreReg OpL, CFibreReg OpR);

	public CFibreScript mScript;
	public int mInstructionPtr;
	public int mFramePtr;
	public CFibreRegStore mData;
	public CFibreRegStore mGlobal;
	public CFibreRegStore mLocal;
	public CFibreRegStore[] mStores;
	public Stack<CFibreCallFrame> mCallFrames;
	public List<CFibreVM.InteropFuncDelegate> mInteropFuncs;

	private static BinaryOpDelegate[,,] _opTable;
	
	static CFibre()
	{
		_opTable = new BinaryOpDelegate[(int)EFibreOpcodeType.COUNT, (int)EFibreType.COUNT, (int)EFibreType.COUNT];

		_opTable[(int)EFibreOpcodeType.NOT, (int)EFibreType.BOOL, 0] = (OpL, OpR) => { return new CFibreReg(!OpL.mBool); };
		_opTable[(int)EFibreOpcodeType.NEG, (int)EFibreType.NUMBER, 0] = (OpL, OpR) => { return new CFibreReg(-OpL.mNumber); };

		_opTable[(int)EFibreOpcodeType.MUL, (int)EFibreType.NUMBER, (int)EFibreType.NUMBER] = (OpL, OpR) => { return new CFibreReg(OpL.mNumber * OpR.mNumber); };
		_opTable[(int)EFibreOpcodeType.DIV, (int)EFibreType.NUMBER, (int)EFibreType.NUMBER] = (OpL, OpR) => { return new CFibreReg(OpL.mNumber / OpR.mNumber); };
		_opTable[(int)EFibreOpcodeType.INC, (int)EFibreType.NUMBER, (int)EFibreType.NUMBER] = (OpL, OpR) => { return new CFibreReg(OpL.mNumber + OpR.mNumber); };
		_opTable[(int)EFibreOpcodeType.DEC, (int)EFibreType.NUMBER, (int)EFibreType.NUMBER] = (OpL, OpR) => { return new CFibreReg(OpL.mNumber - OpR.mNumber); };
		_opTable[(int)EFibreOpcodeType.MOD, (int)EFibreType.NUMBER, (int)EFibreType.NUMBER] = (OpL, OpR) => { return new CFibreReg(OpL.mNumber % OpR.mNumber); };

		_opTable[(int)EFibreOpcodeType.INC, (int)EFibreType.NUMBER, (int)EFibreType.STRING] = (OpL, OpR) => { return new CFibreReg(OpL.mNumber + OpR.mString); };
		_opTable[(int)EFibreOpcodeType.INC, (int)EFibreType.STRING, (int)EFibreType.NUMBER] = (OpL, OpR) => { return new CFibreReg(OpL.mString + OpR.mNumber); };
		_opTable[(int)EFibreOpcodeType.INC, (int)EFibreType.STRING, (int)EFibreType.STRING] = (OpL, OpR) => { return new CFibreReg(OpL.mString + OpR.mString); };
		_opTable[(int)EFibreOpcodeType.INC, (int)EFibreType.STRING, (int)EFibreType.UNDEFINED] = (OpL, OpR) => { return new CFibreReg(OpL.mString + "undefined"); };
		_opTable[(int)EFibreOpcodeType.INC, (int)EFibreType.UNDEFINED, (int)EFibreType.STRING] = (OpL, OpR) => { return new CFibreReg("undefined" + OpR.mString); };
		_opTable[(int)EFibreOpcodeType.INC, (int)EFibreType.STRING, (int)EFibreType.BOOL] = (OpL, OpR) => { return new CFibreReg(OpL.mString + OpR.mBool); };
		_opTable[(int)EFibreOpcodeType.INC, (int)EFibreType.BOOL, (int)EFibreType.STRING] = (OpL, OpR) => { return new CFibreReg(OpL.mBool + OpR.mString); };

		_opTable[(int)EFibreOpcodeType.EQL, (int)EFibreType.UNDEFINED, (int)EFibreType.UNDEFINED] = (OpL, OpR) => { return new CFibreReg(true); };
		_opTable[(int)EFibreOpcodeType.EQL, (int)EFibreType.NUMBER, (int)EFibreType.NUMBER] = (OpL, OpR) => { return new CFibreReg(OpL.mNumber == OpR.mNumber); };
		_opTable[(int)EFibreOpcodeType.EQL, (int)EFibreType.STRING, (int)EFibreType.STRING] = (OpL, OpR) => { return new CFibreReg(OpL.mString == OpR.mString); };
		_opTable[(int)EFibreOpcodeType.EQL, (int)EFibreType.BOOL, (int)EFibreType.BOOL] = (OpL, OpR) => { return new CFibreReg(OpL.mBool == OpR.mBool); };

		_opTable[(int)EFibreOpcodeType.NQL, (int)EFibreType.UNDEFINED, (int)EFibreType.UNDEFINED] = (OpL, OpR) => { return new CFibreReg(false); };
		_opTable[(int)EFibreOpcodeType.NQL, (int)EFibreType.NUMBER, (int)EFibreType.NUMBER] = (OpL, OpR) => { return new CFibreReg(OpL.mNumber != OpR.mNumber); };
		_opTable[(int)EFibreOpcodeType.NQL, (int)EFibreType.STRING, (int)EFibreType.STRING] = (OpL, OpR) => { return new CFibreReg(OpL.mString != OpR.mString); };
		_opTable[(int)EFibreOpcodeType.NQL, (int)EFibreType.BOOL, (int)EFibreType.BOOL] = (OpL, OpR) => { return new CFibreReg(OpL.mBool != OpR.mBool); };

		_opTable[(int)EFibreOpcodeType.LES, (int)EFibreType.NUMBER, (int)EFibreType.NUMBER] = (OpL, OpR) => { return new CFibreReg(OpL.mNumber < OpR.mNumber); };
		_opTable[(int)EFibreOpcodeType.GRT, (int)EFibreType.NUMBER, (int)EFibreType.NUMBER] = (OpL, OpR) => { return new CFibreReg(OpL.mNumber > OpR.mNumber); };
		_opTable[(int)EFibreOpcodeType.LQL, (int)EFibreType.NUMBER, (int)EFibreType.NUMBER] = (OpL, OpR) => { return new CFibreReg(OpL.mNumber <= OpR.mNumber); };
		_opTable[(int)EFibreOpcodeType.GQL, (int)EFibreType.NUMBER, (int)EFibreType.NUMBER] = (OpL, OpR) => { return new CFibreReg(OpL.mNumber >= OpR.mNumber); };

		_opTable[(int)EFibreOpcodeType.AND, (int)EFibreType.BOOL, (int)EFibreType.BOOL] = (OpL, OpR) => { return new CFibreReg(OpL.mBool && OpR.mBool); };
		_opTable[(int)EFibreOpcodeType.LOR, (int)EFibreType.BOOL, (int)EFibreType.BOOL] = (OpL, OpR) => { return new CFibreReg(OpL.mBool || OpR.mBool); };
	}

	public CFibre(CFibreScript Script, CFibreReg FunctionReg, int ArgCount, CFibreRegStore Data, CFibreRegStore Global, CFibreRegStore Local, List<CFibreVM.InteropFuncDelegate> InteropFuncs)
	{
		mScript = Script;		
		mFramePtr = 0;

		mData = Data;
		mGlobal = Global;
		mLocal = Local;
		mInteropFuncs = InteropFuncs;
		mStores = new CFibreRegStore[] { Data, Global, Local };

		mCallFrames = new Stack<CFibreCallFrame>();

		if (FunctionReg == null)
		{
			mInstructionPtr = 0;
			mCallFrames.Push(new CFibreCallFrame(mInstructionPtr, 0, 0));
		}
		else
		{
			if (FunctionReg.mID < -1)
			{
				int funcIndex = -FunctionReg.mID - 10;
				mInteropFuncs[funcIndex](this, 0);
			}
			else
			{
				mInstructionPtr = FunctionReg.mID;
				mCallFrames.Push(new CFibreCallFrame(mInstructionPtr, 0, ArgCount));
			}
		}
	}

	public CFibreReg GetLocalRegister(int Index)
	{
		return mLocal.mStore[Index + mFramePtr];
	}

	public void SetLocalRegister(int Index, CFibreReg Val)
	{
		mLocal.mStore[Index + mFramePtr] = Val;
	}

	public string GetString(int Index, ref bool IsMatch)
	{
		CFibreReg reg = mLocal.mStore[Index + mFramePtr];

		if (reg.mType == EFibreType.STRING)
			return reg.mString;

		IsMatch = false;
		return "";
	}

	public float GetFloat(int Index, ref bool IsMatch)
	{
		CFibreReg reg = mLocal.mStore[Index + mFramePtr];

		if (reg.mType == EFibreType.NUMBER)
			return (float)reg.mNumber;

		IsMatch = false;
		return 0.0f;
	}

	public int GetInt(int Index, ref bool IsMatch)
	{
		CFibreReg reg = mLocal.mStore[Index + mFramePtr];

		if (reg.mType == EFibreType.NUMBER)
			return (int)reg.mNumber;

		IsMatch = false;
		return 0;
	}

	public bool GetBool(int Index, ref bool IsMatch)
	{
		CFibreReg reg = mLocal.mStore[Index + mFramePtr];

		if (reg.mType == EFibreType.BOOL)
			return reg.mBool;

		IsMatch = false;
		return false;
	}

	private CFibreReg _GetRegister(uint Register)
	{
		int index = (int)(Register & 0x3FFFFFFF);
		int type = (int)(Register >> 30);
		CFibreRegStore stack = mStores[type];

		if (type == 2)
			return stack.mStore[index + mFramePtr];
		else
			return stack.mStore[index];
	}

	private void _SetRegister(uint Register, CFibreReg Val)
	{
		int index = (int)(Register & 0x3FFFFFFF);
		int type = (int)(Register >> 30);
		CFibreRegStore stack = mStores[type];

		if (type == 2)
			stack.mStore[index + mFramePtr] = Val;
		else
			stack.mStore[index] = Val;
	}

	private void _LogError(string Message, int Index)
	{
		// TODO: Get proper file name and index char pos from program.
		Debug.LogError("File.fbr(" + Index + "): " + Message);
	}

	public ERunResult Run()
	{
		while (mInstructionPtr != mScript.mProgram.Count)
		{
			// Fetch
			CFibreOpcode op = mScript.mProgram[mInstructionPtr++];

			// Decode & Execute
			switch (op.mOpcode)
			{
				case EFibreOpcodeType.NOP:
					break;
				
				case EFibreOpcodeType.MOV:
				{
					_SetRegister(op.mOperand[0], _GetRegister(op.mOperand[1]));
										
				} break;

				case EFibreOpcodeType.NOT:
				case EFibreOpcodeType.NEG:
				{
					CFibreReg opL = _GetRegister(op.mOperand[1]);
					BinaryOpDelegate opDel = _opTable[(int)op.mOpcode, (int)opL.mType, 0];

					if (opDel == null)
						throw new CFibreRuntimeException("Can't perform operator " + op.mOpcode + " on type " + opL.mType, this);
					
					_SetRegister(op.mOperand[0], opDel(opL, null));
				}
				break;
				
				case EFibreOpcodeType.MUL:
				case EFibreOpcodeType.DIV:
				case EFibreOpcodeType.INC:
				case EFibreOpcodeType.DEC:
				case EFibreOpcodeType.MOD:

				case EFibreOpcodeType.EQL:
				case EFibreOpcodeType.NQL:
				case EFibreOpcodeType.LES:
				case EFibreOpcodeType.GRT:
				case EFibreOpcodeType.LQL:
				case EFibreOpcodeType.GQL:

				case EFibreOpcodeType.AND:
				case EFibreOpcodeType.LOR:
				{
					CFibreReg opL = _GetRegister(op.mOperand[1]);
					CFibreReg opR = _GetRegister(op.mOperand[2]);
					BinaryOpDelegate opDel = _opTable[(int)op.mOpcode, (int)opL.mType, (int)opR.mType];

					if (opDel == null)
						throw new CFibreRuntimeException("Can't perform operator " + op.mOpcode + " on types " + opL.mType + " and " + opR.mType, this);

					_SetRegister(op.mOperand[0], opDel(opL, opR));
				}
				break;

				case EFibreOpcodeType.JMP:
				{
					mInstructionPtr = (int)op.mOperand[0];
				}
				break;

				case EFibreOpcodeType.JIZ:
				{
					CFibreReg opL = _GetRegister(op.mOperand[0]);

					if (opL.mType != EFibreType.BOOL)
						throw new CFibreRuntimeException("Comparison can only operate on a bool, got " + opL.mType, this);

					if (opL.mBool == false)
						mInstructionPtr = (int)op.mOperand[1];
				}
				break;

				case EFibreOpcodeType.JNZ:
				{
					CFibreReg opL = _GetRegister(op.mOperand[0]);

					if (opL.mType != EFibreType.BOOL)
						throw new CFibreRuntimeException("Comparison can only operate on a bool, got " + opL.mType, this);

					if (opL.mBool == true)
						mInstructionPtr = (int)op.mOperand[1];
				}
				break;

				case EFibreOpcodeType.CAL:
				{
					int stackFrameStartIndex = (int)(op.mOperand[1] & 0x3FFFFFFF) + mFramePtr;
					int argCount = (int)op.mOperand[2];
					CFibreReg funcDef = _GetRegister(op.mOperand[0]);

					if (funcDef.mType != EFibreType.FUNCTION)
					{
						throw new CFibreRuntimeException("Tried to call a function but got a " + funcDef.mType, this);
					}
					else
					{
						// TODO: Push data about function so we can reference it in call stack for debugging.
						mCallFrames.Push(new CFibreCallFrame(mInstructionPtr, stackFrameStartIndex, argCount));
						mFramePtr = stackFrameStartIndex;

						if (funcDef.mID < -1)
						{
							int funcIndex = -funcDef.mID - 10;
							CFibreReg returnValue = mInteropFuncs[funcIndex](this, argCount);

							if (returnValue != null && returnValue.mType != EFibreType.COUNT)
								SetLocalRegister(0, returnValue);
							else
								SetLocalRegister(0, mData.mStore[2]);

							CFibreCallFrame frame = mCallFrames.Pop();
							mInstructionPtr = frame.mNextInstruction;

							if (mCallFrames.Count == 0)
								return ERunResult.DONE;

							mFramePtr = mCallFrames.Peek().mFramePtr;

							if (returnValue != null && returnValue.mType == EFibreType.COUNT)
								return ERunResult.CONTINUE;
						}
						else
						{
							mInstructionPtr = funcDef.mID;
						}
					}
				}
				break;

				case EFibreOpcodeType.FNC:
				{
					int argCount = (int)op.mOperand[0];

					// Push undefineds over missing args
					for (int i = mCallFrames.Peek().mPushedArgCount; i < argCount; ++i)
						SetLocalRegister(i, mData.mStore[2]);
				}
				break;

				case EFibreOpcodeType.RET:
				{
					if (op.mOperand[0] == 0)
						SetLocalRegister(0, mData.mStore[2]);

					CFibreCallFrame frame = mCallFrames.Pop();
					mInstructionPtr = frame.mNextInstruction;

					if (mCallFrames.Count == 0)
						return ERunResult.DONE;
					
					mFramePtr = mCallFrames.Peek().mFramePtr;
				}
				break;
			}
		}

		return ERunResult.DONE;
	}

	public void PrintState()
	{
		string output = "";

		output += "Thread:\r\n\t\tPC: " + mInstructionPtr + "\r\n\r\n";
		output += "\t\tFP: " + mFramePtr + "\r\n\r\n";
		output += "Local:\n\r";

		for (int i = 0; i < mLocal.mStore.Length; ++i)
		{
			CFibreReg val = mLocal.mStore[i];

			if (val == null)
				break;

			output += "L" + i + "\t";
			if (i < 100) output += "\t";
			output += val.mType + ", " + val.PrintVal() + "\n\r";
		}

		output += "\r\nCall Frames:\n\r";

		CFibreCallFrame[] callFrames = mCallFrames.ToArray();
		for (int i = 0; i < callFrames.Length; ++i)
		{
			CFibreCallFrame frame = callFrames[i];

			output += "F" + i + "\t";
			if (i < 100) output += "\t";
			output += frame.mNextInstruction + ", " + frame.mFramePtr + "\n\r";
		}

		File.WriteAllText(CGame.DataDirectory + "thread.txt", output);
	}
}

public class CFibreVM
{
	public delegate CFibreReg InteropFuncDelegate(CFibre Fibre, int ArgCount);

	private CFibreScript _script;
	private CFibreRegStore _dataStore;
	private CFibreRegStore _globalStore;
	private List<InteropFuncDelegate> _interopFuncs = new List<InteropFuncDelegate>();

	public CFibreReg Log(CFibre Fibre, int ArgCount)
	{
		if (ArgCount == 1)
			Debug.LogWarning("Fibre Log: " + Fibre.GetLocalRegister(0).PrintVal());

		return null;
	}

	public void PushInteropFunction(string Name, InteropFuncDelegate Func)
	{
		_script.PushInteropFunction(Name, _interopFuncs.Count);
		_interopFuncs.Add(Func);
	}

	public CFibreVM()
	{
		_script = new CFibreScript();
		_script.Init();
		PushInteropFunction("log", Log);
	}

	public void Compile(string ScriptFileName)
	{
		_script.CompileFile(ScriptFileName);
		
		_dataStore = new CFibreRegStore();

		for (int i = 0; i < _script.mData.Count; ++i)
			_dataStore.mStore[i] = _script.mData[i];

		//_script.PrintProgramListing();
	}

	public void ExecuteGlobalFibre()
	{
		_globalStore = new CFibreRegStore();
		CFibre globalFibre = new CFibre(_script, null, 0, _dataStore, _globalStore, _globalStore, _interopFuncs);

		// TODO: Merge with code below
		Debug.LogWarning("Executing global fibre");
		System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
		sw.Start();
		CFibre.ERunResult result = globalFibre.Run();
		sw.Stop();
		Debug.LogWarning("Global fibre completed in " + sw.Elapsed.TotalMilliseconds + "ms");
	}

	public void ExecuteFibre(string FunctionName, CFibreReg[] Args = null)
	{
		CFibreReg funcDef;
		if (!_script.mFunctionDefs.TryGetValue(FunctionName, out funcDef))
			throw new CFibreRuntimeException("Tried to execute undefined fibre function: " + FunctionName);

		int argCount = 0;

		if (Args != null)
			argCount = Args.Length;

		CFibre fibre = new CFibre(_script, funcDef, argCount, _dataStore, _globalStore, new CFibreRegStore(), _interopFuncs);

		for (int i = 0; i < argCount; ++i)
			fibre.SetLocalRegister(i, Args[i]);

		System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
		sw.Start();
		CFibre.ERunResult result = fibre.Run();
		sw.Stop();
		//Debug.LogWarning("FibreVM: (" + FunctionName + ") " + sw.Elapsed.TotalMilliseconds + "ms");
		//Debug.Log("Ret: " + fibre.GetLocalRegister(0).PrintVal());

		//if (result == CFibre.ERunResult.CONTINUE)
			//_fibres.Add(fibre);
	}

	public void ContinueFibre(CFibre Fibre, CFibreReg[] Args = null)
	{
		// Push args from wait result.
		// Can't just push arg to store since frame pointer is at caller local and not callee local.
		// Could stall frame pointer restore until here.

		CFibre.ERunResult result = Fibre.Run();
	}

	public void Serialize()
	{
	}

	public void Deserialize()
	{
	}
}