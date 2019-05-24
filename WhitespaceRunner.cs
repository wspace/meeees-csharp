using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Whitespace
{
    class WhitespaceMain
    {
        static void Main(string[] args)
        {
            //WhitespaceAssembler.WriteWSAsm("hello_world.ws", "hello_world.asm");
            new WhitespaceRunner("../../tests/fib_test.asm").Run();
            Console.ReadLine();
        }
    }

    class WhitespaceRunner
    {
        private static bool fullDebug = false;

        private delegate void ASMCmd(WhitespaceRunner host);
        private static Dictionary<string, ASMCmd> asmToCmd = new Dictionary<string, ASMCmd>
        {
            {"Push" , Push },
            {"Copy", Copy },
            {"Swap", Swap },
            {"Pop", Pop },
            {"Add", Add },
            {"Sub", Sub },
            {"Div", Div },
            {"Mod", Mod },
            {"St", Store },
            {"Ld", Load },
            {"OutC", OutChar },
            {"OutN", OutNum },
            {"ReadC", ReadChar },
            {"ReadN", ReadNum },
            {"Call", Call },
            {"Jump", Jump },
            {"Jz", JumpZero },
            {"Jn", JumpNeg },
            {"Ret", Ret },
            {"End", End }
        };

        private Stack<int> wsStack;
        // a "heap"
        private Dictionary<int, int> wsHeap;


        private Dictionary<int, int> labels;
        private Stack<int> callStack;
        private int paramVal;

        private List<string> fullInsts;
        private int programCounter;
        private bool finished;

        public WhitespaceRunner(string path)
        {
            wsStack = new Stack<int>();
            wsHeap = new Dictionary<int, int>();

            labels = new Dictionary<int, int>();
            callStack = new Stack<int>();
            programCounter = 0;

            finished = false;
            fullInsts = new List<string>();
            LoadInstructions(path);
        }

        public void Step()
        {
            if (finished)
            {
                Console.WriteLine("ERROR: Execution tried to continue after the program was done!");
                return;
            }
            RunCommand(fullInsts[programCounter]);
            programCounter++;
        }

        public void Run()
        {
            while (!finished)
                Step();
        }

        private void LoadInstructions(string path)
        {
            StreamReader inF = null;
            try
            {
                inF = new StreamReader(new FileStream(path, FileMode.Open));
            }
            catch(FileNotFoundException)
            {
                Console.WriteLine(string.Format("ERROR: Couldn't find file {0} to run!", path));
                throw new Exception();
            }
            int count = 0;
            while(!inF.EndOfStream)
            {
                string next = inF.ReadLine();
                if(next.StartsWith("Label"))
                {
                    MakeLabel(Int32.Parse(next.Split(' ')[1]), count);
                }
                else
                {
                    fullInsts.Add(next);
                    count++;
                }
            }
        }

        private void MakeLabel(int num, int pc)
        {
            labels[num] = pc;
        }

        private void RunCommand(string cmd)
        {
            if (fullDebug)
                Console.WriteLine(cmd);
            string[] param = cmd.Split(' ');
            if(param.Length > 1)
            {
                paramVal = Int32.Parse(param[1]);
            }
            asmToCmd[param[0]](this);
        }

        #region Instructions

        static void Push(WhitespaceRunner host)
        {
            host.wsStack.Push(host.paramVal);
        }

        static void Copy(WhitespaceRunner host)
        {
            host.wsStack.Push(host.wsStack.Peek());
        }

        static void Swap(WhitespaceRunner host)
        {
            int tmp = host.wsStack.Pop();
            int tmp2 = host.wsStack.Pop();
            host.wsStack.Push(tmp);
            host.wsStack.Push(tmp2);
        }

        static void Pop(WhitespaceRunner host)
        {
            host.wsStack.Pop();
        }

        static void Add(WhitespaceRunner host)
        {
            host.wsStack.Push(host.wsStack.Pop() + host.wsStack.Pop());
        }

        static void Sub(WhitespaceRunner host)
        {
            int tmp = host.wsStack.Pop();
            host.wsStack.Push(host.wsStack.Pop() - tmp);
        }

        static void Mult(WhitespaceRunner host)
        {
            host.wsStack.Push(host.wsStack.Pop() * host.wsStack.Pop());
        }

        static void Div(WhitespaceRunner host)
        {
            int tmp = host.wsStack.Pop();
            host.wsStack.Push(host.wsStack.Pop() / tmp);
        }

        static void Mod(WhitespaceRunner host)
        {
            int tmp = host.wsStack.Pop();
            host.wsStack.Push(host.wsStack.Pop() % tmp);
        }

        static void Store(WhitespaceRunner host)
        {
            int val = host.wsStack.Pop();
            host.wsHeap[host.wsStack.Pop()] = val;
        }

        static void Load(WhitespaceRunner host)
        {
            int want = host.wsStack.Pop();
            host.wsStack.Push(host.wsHeap[want]);
        }

        static void OutChar(WhitespaceRunner host)
        {
            Console.Write((char)host.wsStack.Pop());
        }

        static void OutNum(WhitespaceRunner host)
        {
            Console.Write(host.wsStack.Pop());
        }

        static void ReadChar(WhitespaceRunner host)
        {
            host.wsHeap[host.wsStack.Pop()] = (Console.Read());
        }

        static void ReadNum(WhitespaceRunner host)
        {
            host.wsHeap[host.wsStack.Pop()] = (Int32.Parse(Console.ReadLine()));
        }

        static void Call(WhitespaceRunner host)
        {
            host.callStack.Push(host.programCounter);
            host.programCounter = host.labels[host.paramVal] - 1;
        }

        static void Jump(WhitespaceRunner host)
        {
            host.programCounter = host.labels[host.paramVal] - 1;
        }

        static void JumpZero(WhitespaceRunner host)
        {
            int test = host.wsStack.Pop();
            if (test == 0)
                host.programCounter = host.labels[host.paramVal] - 1;
        }

        static void JumpNeg(WhitespaceRunner host)
        {
            int test = host.wsStack.Pop();
            if (test < 0)
                host.programCounter = host.labels[host.paramVal] - 1;
        }

        static void Ret(WhitespaceRunner host)
        {
            host.programCounter = host.callStack.Pop();
        }

        static void End(WhitespaceRunner host)
        {
            host.finished = true;
        }

        #endregion
    }
}
