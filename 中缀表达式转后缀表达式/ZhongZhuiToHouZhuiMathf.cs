using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace ZhongZhuiToHouZhui
{
    public class ZhongZhuiToHouZhuiMathf : MonoBehaviour
    {
        //存变量
        private Dictionary<string, float> variables = new();
        //运算符优先级
        private Dictionary<string, int> opPriority = new()
        {
            { "(", 0 },
            { "+", 1 },
            { "-", 1 },
            { "*", 2 },
            { "/", 2 },
            { "%", 2 },
            { "^", 3 },
            { "sin", 4 },
            { "cos", 4 },
            { "tan", 4 }
        };

        //设置变量
        public void SetVariable(string name, float value)
        {
            variables[name] = value;
        }
        //解析
        private List<string> Tokenize(string expression)
        {
            List<string> tokens = new();
            StringBuilder buffer = new();

            for (int i = 0; i < expression.Length; i++)
            {
                char c = expression[i];
                //检查是否是字母
                if (char.IsLetter(c))
                {
                    buffer.Append(c);

                    while (i + 1 < expression.Length && char.IsLetterOrDigit(expression[i + 1]))
                    {
                        buffer.Append(expression[++i]);
                    }
                    string d = buffer.ToString();
                    //判断是否是变量或三角函数
                    if (!variables.ContainsKey(d) && !opPriority.ContainsKey(d))
                    {
                        Debug.Log("“" + d + "”未知！");
                        buffer?.Clear();
                        continue;
                    }
                    tokens.Add(d);
                    buffer?.Clear();
                }
                //检查是否是数字或小数点
                else if (char.IsDigit(c) || c == '.')
                {
                    buffer.Append(c);

                    while (i + 1 < expression.Length && (char.IsDigit(expression[i + 1]) || expression[i + 1] == '.'))
                    {
                        buffer.Append(expression[++i]);
                    }
                    tokens.Add(buffer.ToString());
                    buffer?.Clear();
                }
                //如果非空格
                else if (!char.IsWhiteSpace(c))
                {
                    //判断是否是运算符和括号
                    if (!opPriority.ContainsKey(c.ToString()) && c != ')')
                    {
                        Debug.Log("“" + c + "”未知！");
                        continue;
                    }
                    tokens.Add(c.ToString());
                }
            }

            return tokens;
        }
        //重构
        private List<string> InfixToRPN(List<string> tokens)
        {
            Stack<string> opStack = new();
            List<string> output = new();

            foreach (string token in tokens)
            {
                if (float.TryParse(token, out _) || variables.ContainsKey(token))
                {
                    output.Add(token);
                }
                else if (token == "(")
                {
                    opStack.Push(token);
                }
                else if (token == ")")
                {
                    if (!opStack.Contains("("))
                    {
                        Debug.Log("括号“(”缺失！");
                        return new List<string>(){ "-1" };
                    }
                    while (opStack.Peek() != "(")
                    {
                        output.Add(opStack.Pop());
                    }
                    //移除“(”
                    opStack.Pop();
                }
                //判断优先级
                else
                {
                    while (opStack.Count > 0 && opPriority[token] <= opPriority[opStack.Peek()])
                    {
                        output.Add(opStack.Pop());
                    }
                    opStack.Push(token);
                }
            }
            if (opStack.Contains("(") || output.Contains("("))
            {
                Debug.Log("括号“)”缺失！");
                return new List<string>() { "-1" };
            }
            //+、-、*、/、^、%、sin、cos、tan
            while (opStack.Count > 0)
            {
                output.Add(opStack.Pop());
            }

            return output;
        }
        //计算
        private float CalculateRPN(List<string> rpn)
        {
            Stack<float> stack = new();
            foreach (string token in rpn)
            {
                if (float.TryParse(token, out float num))
                {
                    stack.Push(num);
                }
                else if (variables.TryGetValue(token, out float varValue))
                {
                    stack.Push(varValue);
                }
                else
                {
                    if (opPriority.ContainsKey(token) && opPriority[token] == 4)
                    {
                        float a = stack.Pop();
                        switch (token)
                        {
                            case "sin":
                                stack.Push(Mathf.Sin(a));
                                break;
                            case "cos":
                                stack.Push(Mathf.Cos(a));
                                break;
                            case "tan":
                                stack.Push(Mathf.Tan(a));
                                break;
                        }
                    }
                    else
                    {
                        float b = stack.Pop();
                        float a = stack.Pop();
                        switch (token)
                        {
                            case "+":
                                stack.Push(a + b);
                                break;
                            case "-":
                                stack.Push(a - b);
                                break;
                            case "*":
                                stack.Push(a * b);
                                break;
                            case "/":
                                stack.Push(a / b);
                                break;
                            case "%":
                                stack.Push(a % b);
                                break;
                            case "^":
                                stack.Push(Mathf.Pow(a, b));
                                break;
                        }
                    }
                }
            }
            return stack.Pop();
        }

        //Main函数      如果有变量，应先SetVariable，再调用此函数
        public float Evalute(string expression)
        {
            return CalculateRPN(InfixToRPN(Tokenize(expression)));
        }
    }
}