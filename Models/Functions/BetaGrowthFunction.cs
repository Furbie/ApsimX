﻿using System;
using APSIM.Services.Documentation;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// Takes the value of the child as the x value and returns the y value 
    /// from a beta growth function of the form y = Ymax * (1 + (te - t)/(te-tm))* (t/te)^(te/(te-tm))
    /// See Yin et al 2003 A Flexible Sigmoid Function of Determinate Growth
    /// </summary>
    [Serializable]
    [Description("Takes the value of the child (Ymax adnd Xvalue) and returns the y value from a beta growth function of the form y = Ymax * (1 + (te - t)/(te-tm))* (t/te)^(te/(te-tm))")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class BetaGrowthFunction : Model, IFunction
    {
        /// <summary>Ymax, the maximum value for weight, length or others </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction Ymax = null;
        /// <summary>The thermal time value</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction XValue = null;

        /// <summary>tm, the time at which the maximum growth RATE is obtained  </summary>
        [Description("tm, the time at which the maximum growth rate is obtained")]
        public double tm { get; set; }
        /// <summary>te, the time at which Ymax is obtained </summary>
        [Description("te, the time at which Ymax is obtained")]
        public double te { get; set; }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Error with values to Betagrwoth function</exception>
        public double Value(int arrayIndex = -1)
        {
            try
            {
                //Ymax* (1 + (te - t) / (te - tm)) * (t / te) ^ (te / (te - tm))
                if(XValue.Value(arrayIndex) <= te)
                {
                    return Ymax.Value(arrayIndex) * (1 +
                    (te - XValue.Value(arrayIndex)) /
                    (te - tm)) *
                    Math.Pow(XValue.Value(arrayIndex) / te,
                    te / (te - tm));
                }
                else 
                {
                    return Ymax.Value(arrayIndex);
                }
            }
            catch (Exception)
            {
                throw new Exception("Error with values to beta growth function");
            }
        }
        /// <summary>
        /// Document the model.
        /// </summary>
        /// <param name="indent">Indentation level.</param>
        /// <param name="headingLevel">Heading level.</param>
        protected override IEnumerable<ITag> Document(int indent, int headingLevel)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                Name = this.Name;
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                tags.Add(new AutoDocumentation.Paragraph(" a beta growth function of the form " +
                                                         "y = Ymax * (1 + (te - t)/(te-tm))* (t/te)^(te/(te-tm))", indent));

                // write children.
                foreach (IModel child in this.FindAllChildren<IModel>())
                    AutoDocumentation.DocumentModel(child, tags, 0, indent+1);
            }
        }
    }
}
