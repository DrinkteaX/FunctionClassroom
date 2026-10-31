using System.Collections.Generic;

namespace FunctionClassroom
{
    public sealed class FunctionTemplate
    {
        public FunctionTemplate(string name, string expression, string description)
        {
            Name = name;
            Expression = expression;
            Description = description;
        }

        public string Name { get; private set; }
        public string Expression { get; private set; }
        public string Description { get; private set; }
    }

    public static class TemplateLibrary
    {
        public static IList<FunctionTemplate> All()
        {
            return new List<FunctionTemplate>
            {
                new FunctionTemplate(
                    "二次函数·一般式",
                    "a*x^2+b*x+c",
                    "定义域 R；a 决定开口方向，顶点和对称轴随参数变化。"),
                new FunctionTemplate(
                    "二次函数·顶点式",
                    "a*(x-h)^2+k",
                    "顶点为 (h,k)，对称轴为 x=h；拖动 h、k 可观察平移。"),
                new FunctionTemplate(
                    "反比例函数",
                    "k/x",
                    "定义域 x≠0；k>0 时图像位于第一、三象限。"),
                new FunctionTemplate(
                    "指数函数",
                    "a*b^x",
                    "建议 b>0 且 b≠1；观察底数对增减性和增长速度的影响。"),
                new FunctionTemplate(
                    "对数函数",
                    "a*ln(x-h)+k",
                    "定义域 x>h；与指数函数可配合比较增长和交点。"),
                new FunctionTemplate(
                    "正弦函数",
                    "a*sin(b*x+c)+k",
                    "振幅约为 |a|，周期为 2π/|b|；参数 c、k 控制相位与上下平移。"),
                new FunctionTemplate(
                    "绝对值函数",
                    "a*abs(x-h)+k",
                    "顶点为 (h,k)；a 的符号决定开口方向，绝对值决定折线形态。")
            };
        }
    }
}
