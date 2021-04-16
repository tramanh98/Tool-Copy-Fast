using System;
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

            string readPath = @"D:\YOUR-FOLDER\BLCustomer.Vendor.cs";
            string writePath = @"D:\YOUR-FOLDER\test2.cs";
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
            bool insideDbContext = false;

            int lineNumber = 0;
            using (var streamReader = new StreamReader(readPath))
            {
                while ((line = streamReader.ReadLine()) != null)
                {
                    ++lineNumber;
                    if (line.Contains("//"))
                    {
                        sw.WriteLine(line);
                        continue;
                    }
                    if (line.Contains("{") && line.Contains("}"))
                    {
                        sw.WriteLine(format(line, ref insideDbContext));
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
                        insideDbContext = false;
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
                        if (insideTry)
                        {
                            sw.WriteLine(line);
                        }
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
                            sw.WriteLine(format(line, ref insideDbContext));
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
                                sw.WriteLine(format(line, ref insideDbContext));
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
                        sw.WriteLine(format(line, ref insideDbContext));
                    }
                }
            }
            sw.Close();
        }

        private static string format(string line, ref bool insideDbContext)
        {
            if (line.Contains("model.") && (line.Contains("Add") || line.Contains("Remove")))
            {
                return line;
            }

            if (line.Contains("model.")
                && (line.Contains("ToList()")
                || line.Contains("SingleOrDefault")
                || line.Contains("FirstOrDefault")
                || line.Contains("SaveChanges")
                || line.Contains("Count(")
                || line.Contains("Sum(")
                || line.Contains("Any(")
                )
                )
            {
                insideDbContext = false;
                line = line.Replace("ToList", "ToListAsync");
                line = line.Replace("SingleOrDefault", "SingleOrDefaultAsync");
                line = line.Replace("FirstOrDefault", "FirstOrDefaultAsync");
                line = line.Replace("SaveChanges", "SaveChangesAsync");
                line = line.Replace("Count(", "CountAsync(");
                line = line.Replace("Sum(", "SumAsync(");
                line = line.Replace("Any(", "AnyAsync(");
                line = line.Replace("model.", "await _stmContext.");
                if (line.Contains("foreach("))
                    line = line.Replace("await _stmContext.", "_stmContext.");
                return line;

            }

            // cc: Khoa Nguyễn
            line = line.Replace("CusPart.CAT_Partner.", "CusPart.Partner.");
            line = line.Replace("Partner.CAT_Partner.", "Partner.Partner.");

            if (line.Contains("model."))
            {
                insideDbContext = true;
                line = line.Replace("model.", "await _stmContext.");
            }

            line = line.Replace("await _stmContext.EventAccount", "_stmContext.EventAccount");
            line = line.Replace("await _stmContext.EventRunning", "_stmContext.EventRunning");
            line = line.Replace("System.Data.Entity.EntityState.", "EntityState.");
            

            if (line.Contains("(model,"))
            {
                line = line.Replace("(model,", "(_stmContext,");
            }

            if (line.Contains("ToDataSourceResult"))
                line = line.Replace("ToDataSourceResult", "ToDataSourceResultAsync");

            if (line.Contains("CreateRequest"))
            {
                line = line.Replace("CreateRequestSort", "new DataSourceRequest");
                line = line.Replace("CreateRequest", "new DataSourceRequest");
            }

            if (insideDbContext)
            {
                if(line.Contains("ToList()") || line.Contains("SingleOrDefault()") || line.Contains("FirstOrDefault()"))
                {
                    insideDbContext = false;
                    line = line.Replace("ToList", "ToListAsync");
                    line = line.Replace("SingleOrDefault", "SingleOrDefaultAsync");
                    line = line.Replace("FirstOrDefault", "FirstOrDefaultAsync");
                    return line;
                }
            }
            
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

            var sMethodName = @$"public async Task<{dataType}> {methodNameAndParams}";
            sMethodName = sMethodName.Replace("Task<void>", "Task");
            return sMethodName;
        }

        private static string getMethodName(string method)
        {
            return method.Split('(')[0];
        }
    }
}
