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


        private void ConvertLinesToAssembly(string[] lines)
        {
            RemoveComments(lines);
            SetupPointers();

            foreach (string line in lines)
            {
                string[] parts = line.Split(' ');

                if (parts.Length > 1 || !string.IsNullOrWhiteSpace(parts[0]))
                {
                    if (line.Contains("push"))
                    {
                        ConvertPushToAssembly(parts);
                    }
                    else if (line.Contains("pop"))
                    {
                        ConvertPopToAssembly(parts);
                    }
                    else
                    {
                        ConvertLogicalCommandToAssembly(parts);
                    }
                }
            }
        }

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
