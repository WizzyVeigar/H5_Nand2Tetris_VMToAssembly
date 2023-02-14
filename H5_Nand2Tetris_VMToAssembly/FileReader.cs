using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading.Tasks.Dataflow;

namespace H5_Nand2Tetris_VMToAssembly
{
    internal class FileReader
    {
        StringBuilder assemblyString = new StringBuilder();
        int conditionalCounter = 0;
        int returnCountId = 0;

        //const
        //static
        //Pointer
        //temp

        //set sp 256,        // stack pointer
        //set local 300,     // base address of the local segment
        //set argument 400,  // base address of the argument segment
        //set this 3000,     // base address of the this segment
        //set that 3010,     // base address of the that segment

        public void ReadFile(string path)
        {
            if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path);
                ConvertLinesToAssembly(lines);
            }

            File.WriteAllText(Environment.CurrentDirectory + "/output.asm", assemblyString.ToString());
        }

        /// <summary>
        /// Converts VM lines to assembly code
        /// </summary>
        /// <param name="lines"></param>
        private void ConvertLinesToAssembly(string[] lines)
        {
            RemoveComments(lines);
            SetupPointers();

            foreach (string line in lines)
            {
                string[] parts = line.Split(' ');

                if (parts.Length > 1 || !string.IsNullOrWhiteSpace(parts[0]))
                {
                    switch (parts[0])
                    {
                        case "push":
                            ConvertPushToAssembly(parts);
                            break;
                        case "pop":
                            ConvertPopToAssembly(parts);
                            break;
                        case "label":
                        case "function":
                            CreateLabel(parts);
                            break;
                        case "if-goto":
                            CreateIfGoTo(parts);
                            break;
                        case "goto":
                            CreateGoTo(parts);
                            break;
                        case "return":
                            CreateReturn(parts);
                            break;
                        case "call":
                            CreateCallFunction(parts);
                            break;
                        default:
                            ConvertLogicalCommandToAssembly(parts);
                            break;
                    }
                }
            }
        }

        private void CreateGoTo(string[] parts)
        {
            assemblyString.AppendLine($"@{parts[1]}");
            assemblyString.AppendLine("0;JMP");
        }
        
        private void CreateReturn(string[] parts)
        {
            throw new NotImplementedException();
        }

        private void CreateCallFunction(string[] parts)
        {
            string retAddrLabel = parts[1] + "$" + returnCountId++ + "ret." + parts[2];

            assemblyString.AppendLine("@" + retAddrLabel); //push retAddrLabel
            assemblyString.AppendLine("D=A");
            assemblyString.AppendLine("@SP");
            assemblyString.AppendLine("A=M");
            assemblyString.AppendLine("M=D");
            assemblyString.AppendLine("@SP");
            assemblyString.AppendLine("M=M+1");

            assemblyString.AppendLine("@LCL"); //push LCL
            assemblyString.AppendLine("D=M");
            assemblyString.AppendLine("@SP");
            assemblyString.AppendLine("A=M");
            assemblyString.AppendLine("M=D");
            assemblyString.AppendLine("@SP");
            assemblyString.AppendLine("M=M+1");

            assemblyString.AppendLine("@ARG"); //push ARG
            assemblyString.AppendLine("D=M");
            assemblyString.AppendLine("@SP");
            assemblyString.AppendLine("A=M");
            assemblyString.AppendLine("M=D");
            assemblyString.AppendLine("@SP");
            assemblyString.AppendLine("M=M+1");

            assemblyString.AppendLine("@THIS"); //push THIS
            assemblyString.AppendLine("D=M");
            assemblyString.AppendLine("@SP");
            assemblyString.AppendLine("A=M");
            assemblyString.AppendLine("M=D");
            assemblyString.AppendLine("@SP");
            assemblyString.AppendLine("M=M+1");

            assemblyString.AppendLine("@THAT"); //push THAT
            assemblyString.AppendLine("D=M");
            assemblyString.AppendLine("@SP");
            assemblyString.AppendLine("A=M");
            assemblyString.AppendLine("M=D");
            assemblyString.AppendLine("@SP");
            assemblyString.AppendLine("M=M+1");

            assemblyString.AppendLine("@SP"); // ARG = SP-5-nArgs // Repositions ARG
            assemblyString.AppendLine("D=M");
            assemblyString.AppendLine("@5");
            assemblyString.AppendLine("D=D-A");
            assemblyString.AppendLine("@" + parts[2]);
            assemblyString.AppendLine("D=D-A");
            assemblyString.AppendLine("@ARG");
            assemblyString.AppendLine("M=D");

            assemblyString.AppendLine("@SP"); //LCL = SP // Repositions LCL
            assemblyString.AppendLine("D=M");
            assemblyString.AppendLine("@LCL");
            assemblyString.AppendLine("M=D");

            CreateGoTo(parts); // goto functionName // Transfers control to the called function

            assemblyString.AppendLine("(" + retAddrLabel + ")"); //(retAddrLabel)
        }

        private void CreateLabel(string[] parts)
        {
            assemblyString.AppendLine($"({parts[1]})");
        }

        /// <summary>
        /// Remove comments from the file
        /// </summary>
        /// <param name="lines"></param>
        private void RemoveComments(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("//"))
                {
                    int index = lines[i].IndexOf("//");
                    if (index >= 0)
                    {
                        lines[i] = lines[i].Substring(0, index);
                    }
                }
            }
        }

        /// <summary>
        /// Convert things like add, sub and lg to assembly lines
        /// </summary>
        /// <param name="parts"></param>
        private void ConvertLogicalCommandToAssembly(string[] parts)
        {
            assemblyString.AppendLine("@SP");

            assemblyString.AppendLine("AM=M-1");
            assemblyString.AppendLine("D=M");
            assemblyString.AppendLine("M=0");

            assemblyString.AppendLine("A=A-1");

            switch (parts[0])
            {
                case "add":
                    assemblyString.AppendLine("M=M+D");
                    break;
                case "sub":
                    assemblyString.AppendLine("M=M-D");
                    break;
                case "neg":
                    assemblyString.AppendLine("@0");
                    assemblyString.AppendLine("D=A");
                    assemblyString.AppendLine("@SP");
                    assemblyString.AppendLine("A=M-1");
                    assemblyString.AppendLine("A=A-1");
                    assemblyString.AppendLine("M=D-M");
                    break;
                case "eq":
                    assemblyString.AppendLine("D=M-D");
                    WriteConditional("D;JNE");
                    break;
                case "gt":
                    assemblyString.AppendLine("D=M-D");
                    WriteConditional("D;JLE");
                    break;
                case "lt":
                    assemblyString.AppendLine("D=M-D");
                    WriteConditional("D;JGE");
                    break;
                case "and":
                    assemblyString.AppendLine("M=M&D");
                    break;
                case "or":
                    assemblyString.AppendLine("M=M|D");
                    break;
                case "not":
                    assemblyString.AppendLine("M=!M");
                    break;
            }
        }


        /// <summary>
        /// Creates a if-goto to loop back on a previous label. Used for stuff like loops
        /// </summary>
        /// <param name="parts"></param>
        private void CreateIfGoTo(string[] parts)
        {
            //Place to look for loop amount
            assemblyString.AppendLine("@ARG");
            assemblyString.AppendLine("A=M");
            assemblyString.AppendLine("MD=M-1");
            assemblyString.AppendLine($"@{parts[1]}");
            assemblyString.AppendLine("D;JNE");
        }

        /// <summary>
        /// Method for writing a conditional >, <, ==
        /// </summary>
        /// <param name="jumpCondition"></param>
        private void WriteConditional(string jumpCondition)
        {
            assemblyString.AppendLine("@FALSE" + conditionalCounter);
            assemblyString.AppendLine(jumpCondition);
            assemblyString.AppendLine("@SP");
            assemblyString.AppendLine("A=M-1");
            assemblyString.AppendLine("M=-1");
            assemblyString.AppendLine("@CONTINUE" + conditionalCounter);
            assemblyString.AppendLine("0;JMP");
            assemblyString.AppendLine($"(FALSE{conditionalCounter})");
            assemblyString.AppendLine("@SP");
            assemblyString.AppendLine("A=M-1");
            assemblyString.AppendLine("M=0");
            assemblyString.AppendLine($"(CONTINUE{conditionalCounter})");

            conditionalCounter++;
        }

        /// <summary>
        /// Convert a line that has been split into parts, into a push command equivelant in Assembly
        /// </summary>
        /// <param name="parts"></param>
        private void ConvertPushToAssembly(string[] parts)
        {
            bool isOnStack = false;
            string destAddress = "DESTADDRESS HAD A FAILURE";

            switch (parts[1])
            {
                case "constant":
                    destAddress = "@SP";
                    isOnStack = true;
                    break;
                case "local":
                    destAddress = "@LCL";
                    break;
                case "argument":
                    destAddress = "@ARG";
                    break;
                case "this":
                    destAddress = "@THIS";
                    break;
                case "that":
                    destAddress = "@THAT";
                    break;
                case "static":
                    destAddress = "@STATIC";
                    break;
                case "temp":
                    destAddress = "@TEMP";
                    break;
                case "pointer":
                    if (parts[2] == "0")
                        destAddress = "@THIS";
                    else
                        destAddress = "@THAT";
                    break;
            }

            //If push is to constant
            if (isOnStack)
            {
                assemblyString.AppendLine("@" + parts[2]);
                assemblyString.AppendLine("D=A");

                assemblyString.AppendLine(destAddress);
                assemblyString.AppendLine("A=M");
                assemblyString.AppendLine("M=D");

                assemblyString.AppendLine(destAddress);
                assemblyString.AppendLine("M=M+1");
            }
            //If push is to other area
            else
            {
                assemblyString.AppendLine("@" + parts[2]);
                assemblyString.AppendLine("D=A");

                assemblyString.AppendLine(destAddress);
                assemblyString.AppendLine("A=M+D");
                //Get value from other adddress
                assemblyString.AppendLine("D=M");

                assemblyString.AppendLine("@SP");
                assemblyString.AppendLine("A=M");
                assemblyString.AppendLine("M=D");

                assemblyString.AppendLine("@SP");
                assemblyString.AppendLine("M=M+1");
            }
        }

        /// <summary>
        /// Pop the last value from the stack onto memory
        /// </summary>
        /// <param name="parts"></param>
        private void ConvertPopToAssembly(string[] parts)
        {
            string destAddress = "POP DESTADDRESS HAD A FAILURE";

            switch (parts[1])
            {
                //case "constant":
                //    destAddress = "@SP";
                //    break;
                case "local":
                    destAddress = "@LCL";
                    break;
                case "argument":
                    destAddress = "@ARG";
                    break;
                case "this":
                    destAddress = "@THIS";
                    break;
                case "that":
                    destAddress = "@THAT";
                    break;
                case "static":
                    destAddress = "@STATIC";
                    break;
                case "temp":
                    destAddress = "@TEMP";
                    break;
                case "pointer":
                    if (parts[2] == "0")
                        destAddress = "@THIS";
                    else
                        destAddress = "@THAT";
                    break;
            }

            assemblyString.AppendLine("@" + parts[2]);
            assemblyString.AppendLine("D=A");

            assemblyString.AppendLine(destAddress);
            assemblyString.AppendLine("A=M+D");
            assemblyString.AppendLine("D=M");

            assemblyString.AppendLine("@SP");
            assemblyString.AppendLine("A=M");
            assemblyString.AppendLine("M=D");

            assemblyString.AppendLine("@SP");
            assemblyString.AppendLine("M=M+1");
        }

        /// <summary>
        /// Setup the predefined pointers 
        /// </summary>
        private void SetupPointers()
        {
            assemblyString.AppendLine("@256");
            assemblyString.AppendLine("D=A");
            assemblyString.AppendLine("@SP");
            assemblyString.AppendLine("M=D");

            assemblyString.AppendLine("@300");
            assemblyString.AppendLine("D=A");
            assemblyString.AppendLine("@LCL");
            assemblyString.AppendLine("M=D");

            assemblyString.AppendLine("@400");
            assemblyString.AppendLine("D=A");
            assemblyString.AppendLine("@ARG");
            assemblyString.AppendLine("M=D");

            assemblyString.AppendLine("@3000");
            assemblyString.AppendLine("D=A");
            assemblyString.AppendLine("@THIS");
            assemblyString.AppendLine("M=D");

            assemblyString.AppendLine("@3010");
            assemblyString.AppendLine("D=A");
            assemblyString.AppendLine("@THAT");
            assemblyString.AppendLine("M=D");
        }
    }
}
