using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            //ControllerGeneration();
            BL_Generation.Generate();
        }

        private static void ControllerGeneration()
        {
            string readPath = @"D:\YOUR-FOLDER\CUS.cs";
            string writePath = @"D:\YOUR-FOLDER\test.cs";
            string line;
            var sw = new StreamWriter(writePath);

            var stack = new Stack<string>();

            string try_string = "try";
            string serviceFactory_string = "ServiceFactory";
            string catch_string = "catch";
            string throw_string = "throw";
            bool insideTry = false;

            int lineNumber = 0;
            using (var streamReader = new StreamReader(readPath))
            {
                while ((line = streamReader.ReadLine()) != null)
                {
                    ++lineNumber;
                    if (line.Contains("{") && line.Contains("}"))
                    {
                        sw.WriteLine(line);
                        continue;
                    }
                    else if (line.Contains("//"))
                    {
                        continue;
                    }
                    else if (line.Contains("using"))
                    {
                        sw.WriteLine(line);
                    }
                    else if (string.IsNullOrWhiteSpace(line))
                    {
                        sw.WriteLine(line);
                    }
                    else if (line.Contains("namespace"))
                    {
                        sw.WriteLine(line);
                    }
                    else if (line.Contains("public partial class"))
                    {
                        sw.WriteLine(line);
                    }
                    else if (line.Contains("public"))
                    {
                        sw.WriteLine(GenerateMethodName(line));
                    }
                    else if (line.TrimStart().StartsWith(try_string))
                    {
                        if (insideTry)
                        {
                            sw.WriteLine(line);
                        }
                        else
                        {
                            insideTry = true;
                            stack.Push(try_string);
                        }
                    }
                    else if (line.Contains(serviceFactory_string))
                    {
                        stack.Push(serviceFactory_string);
                    }
                    else if (line.Contains(catch_string))
                    {
                        if (insideTry)
                        {
                            sw.WriteLine(line);
                        }
                        else
                        {
                            stack.Push(catch_string);
                        }
                    }
                    else if (line.Contains(throw_string))
                    {
                        continue;
                    }
                    else if (line.Contains("HttpPost"))
                    {
                        continue;
                    }
                    else if (line.Contains("{"))
                    {
                        if (stack.Any())
                        {
                            string element = stack.Peek();

                            if (element == try_string || element == serviceFactory_string || element == catch_string)
                            {
                                stack.Push("{");
                                continue;
                            }
                            else
                            {
                                sw.WriteLine(line);
                                stack.Push("{");
                            }
                        }
                        else
                        {
                            sw.WriteLine(line);
                        }
                    }
                    else if (line.Contains("}"))
                    {
                        if (!stack.Any())
                        {
                            sw.WriteLine(line);
                            continue;
                        }

                        var element = stack.Peek();
                        if (element != "{")
                        {
                            throw new Exception($"{lineNumber}_{line}");
                        }
                        else
                        {
                            stack.Pop();

                            if (stack.Any() && (stack.Peek() == try_string || stack.Peek() == serviceFactory_string || stack.Peek() == catch_string))
                            {
                                if (stack.Peek() == try_string)
                                {
                                    insideTry = false;
                                }
                                stack.Pop();
                            }
                            else
                            {
                                sw.WriteLine(line);
                            };
                        }
                    }
                    else
                    {
                        sw.WriteLine(line);
                    }
                }
            }
            sw.Close();
        }

        private static string GenerateMethodName(string line)
        {
            string[] tokens = line.Split(' ');
            
            int index = 0;

            while (tokens[index] == "")
            {
                ++index;
            }

            string accessModifier = tokens[index];

            ++index;

            while (tokens[index] == "")
            {
                ++index;
            }

            string dataType = tokens[index];

            ++index;

            string methodNameAndParams = "";
            string methodName = getMethodName(tokens[index]);

            while (index < tokens.Length)
            {
                methodNameAndParams = $"{methodNameAndParams} {tokens[index]}";
                ++index;
            }

            return @$"[HttpPost(""{methodName}"")]
                public async Task<IActionResult> {methodNameAndParams}";
        }

        private static string getMethodName(string method)
        {
            return method.Split('(')[0];
        }
    }
}
