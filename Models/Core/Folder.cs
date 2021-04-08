﻿namespace Models.Core
{
    using Models.Core.Run;
    using Models.Factorial;
    using Models;
    using Models.PMF;
    using Models.PMF.Interfaces;
    using System;
    using APSIM.Services.Documentation;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    /// <summary>
    /// A folder model
    /// </summary>
    [ViewName("UserInterface.Views.FolderView")]
    [PresenterName("UserInterface.Presenters.FolderPresenter")]
    [ScopedModel]
    [Serializable]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Folder))]
    [ValidParent(ParentType = typeof(Simulations))]
    [ValidParent(ParentType = typeof(Experiment))]
    [ValidParent(ParentType = typeof(IOrgan))]
    [ValidParent(ParentType = typeof(Morris))]
    [ValidParent(ParentType = typeof(Sobol))]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    public class Folder : Model
    {
        /// <summary>Show page of graphs?</summary>
        public bool ShowPageOfGraphs { get; set; }

        /// <summary>Constructor</summary>
        public Folder()
        {
            ShowPageOfGraphs = true;
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
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                if (ShowPageOfGraphs)
                {
                    foreach (Memo memo in FindAllChildren<Memo>())
                        memo.Document(tags, headingLevel, indent);

                    if (FindAllChildren<Experiment>().Any())
                    {
                        // Write Phase Table
                        tags.Add(new AutoDocumentation.Paragraph("**List of experiments.**", indent));
                        DataTable tableData = new DataTable();
                        tableData.Columns.Add("Experiment Name", typeof(string));
                        tableData.Columns.Add("Design (Number of Treatments)", typeof(string));

                        foreach (IModel child in FindAllChildren<Experiment>())
                        {
                            IModel Factors = child.FindChild<Factors>();
                            string Design = GetTreatmentDescription(Factors);
                            foreach (Permutation permutation in Factors.FindAllChildren<Permutation>())
                                Design += GetTreatmentDescription(permutation);

                            var simulationNames = (child as Experiment).GenerateSimulationDescriptions().Select(s => s.Name);
                            Design += " (" + simulationNames.ToArray().Length + ")";

                            DataRow row = tableData.NewRow();
                            row[0] = child.Name;
                            row[1] = Design;
                            tableData.Rows.Add(row);
                        }
                        tags.Add(new AutoDocumentation.Table(tableData, indent));

                    }
                    int pageNumber = 1;
                    int i = 0;
                    List<Graph> children = FindAllChildren<Graph>().ToList();
                    while (i < children.Count)
                    {
                        GraphPage page = new GraphPage();
                        page.name = Name + pageNumber;
                        for (int j = i; j < i + 6 && j < children.Count; j++)
                            if (children[j].IncludeInDocumentation)
                                page.graphs.Add(children[j] as Graph);
                        if (page.graphs.Count > 0)
                            tags.Add(page);
                        i += 6;
                        pageNumber++;
                    }

                    // Document everything else other than graphs
                    foreach (IModel model in Children)
                        if (!(model is Graph) && !(model is Memo))
                            AutoDocumentation.DocumentModel(model, tags, headingLevel + 1, indent);
                }
                else
                {
                    foreach (IModel model in Children)
                        AutoDocumentation.DocumentModel(model, tags, headingLevel + 1, indent);
                }
            }
        }

        private string GetTreatmentDescription(IModel factors)
        {
            string design = "";
            foreach (Factor factor in factors.FindAllChildren<Factor>())
            {
                if (design != "")
                    design += " x ";
                design += factor.Name;
            }
            return design;
        }
    }
}
