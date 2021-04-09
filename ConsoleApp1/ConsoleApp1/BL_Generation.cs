﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class BL_Generation
    {
        public static void Generate()
        {

            string readPath = @"D:\NhatThiCute\BLCustomer.Vendor.cs";
            string writePath = @"D:\NhatThiCute\test2.cs";
            string line;
            var sw = new StreamWriter(writePath);

            var stack = new Stack<string>();

            string try_string = "try";
            string serviceFactory_string = "ServiceFactory";
            string catch_string = "catch";
            string throw_string = "throw";
            string using_string = "using";
            string final_string = "finally";
            string data_entities_string = "DataEntities";
            bool insideTry = false;
            bool insideCatch = false;
            bool insideFinally = false;

            int lineNumber = 0;
            using (var streamReader = new StreamReader(readPath))
            {
                while ((line = streamReader.ReadLine()) != null)
                {
                    ++lineNumber;
                    if (line.Contains("//"))
                    {
                        line = line.Split("//")[0];

                        if (string.IsNullOrWhiteSpace(line))
                            continue;
                    }
                    if (line.Contains("{") && line.Contains("}"))
                    {
                        sw.WriteLine(line);
                        continue;
                    }
                    else if (line.Contains(using_string) && line.Contains(data_entities_string))
                    {
                        stack.Push(using_string);
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
                            insideCatch = true;
                            stack.Push(catch_string);
                        }
                    }
                    else if (line.Contains(final_string))
                    {
                        if (insideTry)
                        {
                            sw.WriteLine(line);
                        }
                        else
                        {
                            insideFinally = true;
                            stack.Push(final_string);
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

                            if (element == try_string || element == serviceFactory_string || element == catch_string || element == using_string || element == final_string)
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
                            sw.WriteLine(format(line));
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

                            if (stack.Any() && (stack.Peek() == try_string || stack.Peek() == serviceFactory_string || stack.Peek() == catch_string || stack.Peek() == using_string || stack.Peek() == final_string))
                            {
                                if (stack.Peek() == try_string)
                                {
                                    insideTry = false;
                                }
                                if (stack.Peek() == catch_string)
                                {
                                    insideCatch = false;
                                }
                                if (stack.Peek() == final_string)
                                {
                                    insideFinally = false;
                                }
                                stack.Pop();
                            }
                            else
                            {
                                sw.WriteLine(format(line));
                            };
                        }
                    }
                    else
                    {
                        // pass
                        if (insideCatch)
                        {
                            continue; 
                        }
                        if (insideFinally)
                        {
                            continue;
                        }
                        if (line.Contains("HelperLog.Method_Start"))
                        {
                            continue;
                        }
                        sw.WriteLine(format(line));
                    }
                }
            }
            sw.Close();
        }

        private static string format(string line)
        {
            if (line.Contains("ToDataSourceResult"))
                return line.Replace("ToDataSourceResult", "ToDataSourceResultAsync");
            if (line.Contains("model."))
                line.Replace("model.", "await _stmContext.");
            return line;
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

            return @$"public async Task<{dataType}> {methodNameAndParams}";
        }

        private static string getMethodName(string method)
        {
            return method.Split('(')[0];
        }
    }
}
