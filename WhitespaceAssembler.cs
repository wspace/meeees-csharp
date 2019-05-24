using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

/**
 * Let S[n] be the value n from the top of the stack
 * INSTRUCTION SET
 * -- STACK --
 * Push [Number]        // Push a value onto the stack
 * Copy                 // Duplicate the value on top of the stack
 * Swap                 // Swap the top two values on the stack
 * Pop                  // Pop a value off the stack
 * 
 *  -- ARITHMETIC --
 * Add                  // Push(S[1] + S[0])
 * Sub                  // Push(S[1] - S[0])
 * Mult                 // Push(S[1] * S[0])
 * Div                  // Push(S[1] / S[0]) (integer division)
 * Mod                  // Push(S[1] % S[0])
 * 
 *  -- HEAP --
 *  St                  // Heap[S[1]] = S[0]
 *  Ld                  // Push(Heap[S[0]])
 *  
 *  -- IO --
 * OutC                 // PrintChar(S[0])
 * OutN                 // PrintNum(S[0])
 * ReadC                // Heap[S[0]] = ReadChar()
 * ReadN                // Heap[S[0]] = ReadNum()
 * 
 * -- FLOW --
 * Label [Number]       
 * Call [Number]        // call subroutine
 * Jump [Number]        
 * Jz [Number]          // Jump if S[0] == 0
 * Jn [Number]          // Jump if S[0] less than 0
 * Ret                  // end subroutine
 * End                  // program over
 */

namespace Whitespace
{
    class WhitespaceAssembler
    {

        public static bool debug = true;
        public static bool deep_debug = true;

        public static int GetNextChar(FileStream inS, bool canEnd = false)
        {
            int next = inS.ReadByte();

            while(true)
            {
                if (next == -1)
                {
                    if (canEnd)
                    {
                        return -1;
                    }
                    else
                    {
                        throw new Exception("Error! End of file reached while parsing an intstruction!");
                    }
                }
                switch ((char) next)
                {
                    case ' ':
                        if (deep_debug)
                            Console.WriteLine("[Space]");
                        return 0;
                    case '\t':
                        if (deep_debug)
                            Console.WriteLine("[Tab]");
                        return 1;
                    case '\n':
                        if (deep_debug)
                            Console.WriteLine("[LF]");
                        return 2;
                    default:
                        break;
                }
                next = inS.ReadByte();
            }
        }

        public static void WriteWSAsm(string inPath, string outPath)
        {
            FileStream inS = null;
            StreamWriter outS = null;
            try
            {
                inS = new FileStream(inPath, FileMode.Open);
            }
            catch(FileNotFoundException)
            {
                Console.WriteLine(string.Format("File not found at: {0}", inPath));
                throw new Exception();
            }
            try
            {
                outS = new StreamWriter(new FileStream(outPath, FileMode.Create));
            }
            catch(IOException)
            {
                Console.WriteLine(string.Format("Error creating file at: {0}", outPath));
                throw new Exception();
            }
            ASMBuilder builder = new ASMBuilder();
            if (debug)
                Console.WriteLine("Initialization & File Opening done, beginning parsing");
            int next = GetNextChar(inS, true);
            while(next != -1)
            {
                builder.ParseInstruction(next, inS);
                next = GetNextChar(inS, true);
            }
            if(builder.IsDone())
            {
                List<string> asm = builder.GetInsts();
                foreach(string s in asm)
                {
                    outS.WriteLine(s);
                }
                outS.Close();
            }
            else
            {
                throw new Exception("EOF reached but builder was not finished!");
            }
        }
    }

    class ASMBuilder
    {
        List<string> insts;
        bool done;

        public ASMBuilder()
        {
            insts = new List<string>();
            done = false;
        }

        public bool IsDone()
        {
            return done;
        }

        public List<string> GetInsts()
        {
            return insts;
        }

        public void ParseInstruction(int n, FileStream inS)
        {
            if(done)
            {
                throw new Exception("Instructions continue after end of program reached!");
            }
            switch (n)
            {
                // stack manip
                case 0:
                    insts.Add(ParseStack(inS));
                    break;
                // arithmetic, heap, or I/O
                case 1:
                    switch(WhitespaceAssembler.GetNextChar(inS))
                    {
                        case 0:
                            insts.Add(ParseArithmetic(inS));
                            break;
                        case 1:
                            insts.Add(ParseHeap(inS));
                            break;
                        case 2:
                            insts.Add(ParseIO(inS));
                            break;
                    }
                    break;
                // flow control
                case 2:
                    insts.Add(ParseFlow(inS));
                    break;


            }
            if(WhitespaceAssembler.debug)
                Console.WriteLine("Finished Instruction: " + insts[insts.Count - 1]);
        }

        private string ParseStack(FileStream inS)
        {
            switch (WhitespaceAssembler.GetNextChar(inS))
            {
                case 0:
                    return "Push " + ParseNum(inS).ToString();
                case 1:
                    throw new Exception("Parse Stack Manipultion encountered an invalid operand! [Tab]");
                case 2:
                    switch(WhitespaceAssembler.GetNextChar(inS))
                    {
                        case 0:
                            return "Copy";
                        case 1:
                            return "Swap";
                        case 2:
                            return "Pop";
                    }
                    throw new Exception("Parse Stack Manipulation followed by [LF] encountered an unknown operand!");
            }
            throw new Exception("Parse Stack Manipulation encountered an unknown operand!");

        }

        private string ParseArithmetic(FileStream inS)
        {
            switch(WhitespaceAssembler.GetNextChar(inS))
            {
                case 0:
                    switch(WhitespaceAssembler.GetNextChar(inS))
                    {
                        case 0:
                            return "Add";
                        case 1:
                            return "Sub";
                        case 2:
                            return "Mult";
                    }
                    throw new Exception("Parse Arithmetic followed by [Space] encountered an unknown operand!");
                case 1:
                    switch (WhitespaceAssembler.GetNextChar(inS))
                    {
                        case 0:
                            return "Div";
                        case 1:
                            return "Mod";
                        case 2:
                            throw new Exception("Parse Arithmetic followed by [Tab] encountered an invalid operand! [LF]");
                    }
                    throw new Exception("Parse Arithmetic followed by [Tab] encountered an unknown operand!");
                case 2:
                    throw new Exception("Parse Arithmetic encountered an invalid operand! [LF]");
            }
            throw new Exception("Parse Arithmetic encountered an unknown operand!");
        }

        private string ParseHeap(FileStream inS)
        {
            switch(WhitespaceAssembler.GetNextChar(inS))
            {
                case 0:
                    return "St";
                case 1:
                    return "Ld";
                case 2:
                    throw new Exception("Parse Heap encountered an invalid operand! [LF]");
            }
            throw new Exception("Parse Heap encountered an unknown operand!");
        }

        private string ParseIO(FileStream inS)
        {
            switch (WhitespaceAssembler.GetNextChar(inS))
            {
                case 0:
                    switch (WhitespaceAssembler.GetNextChar(inS))
                    {
                        case 0:
                            return "OutC";
                        case 1:
                            return "OutN";
                        case 2:
                            throw new Exception("Parse IO followed by [Space] encountered an invalid operand! [LF]");
                    }
                    throw new Exception("Parse IO followed by [Space] encountered an unknown operand!");
                case 1:
                    switch(WhitespaceAssembler.GetNextChar(inS))
                    {
                        case 0:
                            return "ReadC";
                        case 1:
                            return "ReadN";
                        case 2:
                            throw new Exception("Parse IO followed by [Tab] encountered an invalid operand! [LF]");
                    }
                    throw new Exception("Parse IO followed by [Tab] encountered an unknown operand!");
                case 2:
                    throw new Exception("Parse IO encountered an invalid operand! [LF]");
            }
            throw new Exception("Parse IO encountered an unknown operand!");
        }

        private string ParseFlow(FileStream inS)
        {
            switch (WhitespaceAssembler.GetNextChar(inS))
            {
                case 0:
                    switch (WhitespaceAssembler.GetNextChar(inS))
                    {
                        case 0:
                            return "Label " + ParseNum(inS).ToString();
                        case 1:
                            return "Call " + ParseNum(inS).ToString();
                        case 2:
                            return "Jump " + ParseNum(inS).ToString();
                    }
                    throw new Exception("Parse Flow followed by [Space] encountered an unknown operand!");
                case 1:
                    switch (WhitespaceAssembler.GetNextChar(inS))
                    {
                        case 0:
                            return "Jz " + ParseNum(inS).ToString();
                        case 1:
                            return "Jn " + ParseNum(inS).ToString();
                        case 2:
                            return "Ret";
                    }
                    throw new Exception("Parse Flow followed by [Tab] encountered an unknown operand!");
                case 2:
                    switch (WhitespaceAssembler.GetNextChar(inS))
                    {
                        case 0:
                            throw new Exception("Parse Flow followed by [LF] encountered an invalid operand! [Space]");
                        case 1:
                            throw new Exception("Parse Flow followed by [LF] encountered an invalid operand! [Tab]");
                        case 2:
                            done = true;
                            return "End";
                    }
                    throw new Exception("Parse Flow followed by [LF] encountered an unknown operand!");

            }
            throw new Exception("Parse Flow encountered an unknown operand!");

        }

        private int ParseNum(FileStream inS)
        {
            bool done = false;
            int signCheck = WhitespaceAssembler.GetNextChar(inS);
            if (signCheck == 2)
                throw new Exception("Parse Number encountered an invalid sign! [LF]");
            bool positive = signCheck == 0;
            int res = 0;
            int bitCount = 0;
            while(!done)
            {
                bitCount++;
                if (bitCount > 31)
                    throw new Exception("Parse Number encountered a number larger than 31 bits!");
                switch(WhitespaceAssembler.GetNextChar(inS))
                {
                    case 0:
                        res <<= 1;
                        break;
                    case 1:
                        res <<= 1;
                        res += 1;
                        break;
                    case 2:
                        done = true;
                        break;
                }
            }
            return positive ? res : -res;
        }
    }
}
