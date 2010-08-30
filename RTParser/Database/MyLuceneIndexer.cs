using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using LAIR.Collections.Generic;
using LAIR.ResourceAPIs.WordNet;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using MushDLR223.ScriptEngines;
using MushDLR223.Utilities;
using MushDLR223.Virtualization;

namespace RTParser.Database
{
    public class MyLuceneIndexer
    {
        private const string DOC_ID_FIELD_NAME = "ID_FIELD";
        private const string HYPO_FIELD_NAME = "HYPO_FIELD";

        private string _fieldName;
        private string _indexDir;
        private ulong _docid=0;
        private static System.IO.FileInfo _path;
        public WordNetEngine wordNetEngine;

        Lucene.Net.Store.Directory _directory;// = new RAMDirectory();
        Analyzer _analyzer;// = new StandardAnalyzer();

        public MyLuceneIndexer(string indexDir, string fieldName)
        {
            _indexDir = indexDir;
            _fieldName = fieldName;
            _path = new System.IO.FileInfo(indexDir);        
            //_directory = new RAMDirectory(); 
            _analyzer = new StandardAnalyzer();
            if (IndexReader.IndexExists(_indexDir))
            {
                try
                {
                    _directory = FSDirectory.GetDirectory(_indexDir, false);
                    IndexReader indexReader = IndexReader.Open(_indexDir);
                    _docid = (ulong)indexReader.NumDocs() + 1;
                    indexReader.Close();
                }
                catch(Exception e)
                {
                }
            }
            else
            {
                _directory = FSDirectory.GetDirectory(_indexDir, true);
            }


            
        }

        public ulong IncDocId()
        {
            return ++_docid;
        }
        /// <summary>
        /// This method indexes the content that is sent across to it. Each piece of content (or "document")
        /// that is indexed has to have a unique identifier (so that the caller can take action based on the
        /// document id). Therefore, this method accepts key-value pairs in the form of a dictionary. The key
        /// is a ulong which uniquely identifies the string to be indexed. The string itself is the value
        /// within the dictionary for that key. Be aware that stop words (like the, this, at, etc.) are _not_
        /// indexed.
        /// </summary>
        /// <param name="txtIdPairToBeIndexed">A dictionary of key-value pairs that are sent by the caller
        /// to uniquely identify each string that is to be indexed.</param>
        /// <returns>The number of documents indexed.</returns>
        public int Index(Dictionary<ulong, string> txtIdPairToBeIndexed)
        {
            bool indexExists = IndexReader.IndexExists(_directory); 
            bool createIndex = !indexExists;
            IndexWriter indexWriter = new IndexWriter(_directory, _analyzer, createIndex);
            indexWriter.SetUseCompoundFile(false);

            Dictionary<ulong, string>.KeyCollection keys = txtIdPairToBeIndexed.Keys;

            foreach (ulong id in keys)
            {
                string text = txtIdPairToBeIndexed[id];

                Document document = new Document();
                // Raw text
                Field bodyField = new Field(_fieldName, text, Field.Store.YES, Field.Index.TOKENIZED);
                document.Add(bodyField);
                // Undisambiguated Hyponyms of nouns in text
                string wn_hypo = WordNetExpand(text,true);
                Field wnHypoField = new Field(HYPO_FIELD_NAME, wn_hypo, Field.Store.YES, Field.Index.TOKENIZED);
                document.Add(wnHypoField);
                // The doc ID
                Field idField = new Field(DOC_ID_FIELD_NAME, (id).ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED);
                document.Add(idField);

                indexWriter.AddDocument(document);
            }

            int numIndexed = indexWriter.DocCount();
            indexWriter.Optimize();
            indexWriter.Close();

            return numIndexed;
        }

        public void Insert(string myText)
        {
            
            ulong myDocID = IncDocId();
            Dictionary<ulong, string> contentIdPairs = new Dictionary<ulong, string>();
            contentIdPairs.Add(myDocID, myText);

            // Indexing:
            int numIndexed = Index(contentIdPairs);
        }
        /// <summary>
        /// This method searches for the search query, then deletes the top ranked and inserts.
        /// </summary>
        /// <param name="query">The search term as a string that the caller wants to search for within the
        /// index as referenced by this object.</param>
        /// <param name="myText">The new value to replace in the database.</param>

        public void Update(string searchQuery, string myText)
        {
            // Searching:
            ulong[] ids;
            string[] results;
            float[] scores;

            int numHits;
            // find it
            Console.WriteLine("Replacing best \"{0}\"...", searchQuery);
            //Search(query, out ids, out results, out scores);
            IndexSearcher indexSearcher = new IndexSearcher(_directory);
            try
            {
                QueryParser queryParser = new QueryParser(_fieldName, _analyzer);
                Query query = queryParser.Parse(searchQuery);
                Hits hits = indexSearcher.Search(query);
                numHits = hits.Length();

                // if we want to do something smarter later
                //ids = new ulong[numHits];
                //results = new string[numHits];
                //scores = new float[numHits];
                //for (int i = 0; i < numHits; ++i)
                //{
                //    float score = hits.Score(i);
                //    string text = hits.Doc(i).Get(_fieldName);
                //    string idAsText = hits.Doc(i).Get(MyLuceneIndexer.DOC_ID_FIELD_NAME);
                //    ids[i] = UInt64.Parse(idAsText);
                //    results[i] = text;
                //    scores[i] = score;
                //}
                if (numHits > 0)
                {
                    indexSearcher.GetIndexReader().DeleteDocument(0);
     
                }

            }
            finally
            {
                indexSearcher.Close();
            }

            Insert(myText);
        }



        public void LoadFileByLines(string filename)
        {
            string line;
            long linecount = 0;

            if (HostSystem.FileExists(filename))
            {
                string absoluteFileName = HostSystem.GetAbsolutePath(filename);
                System.IO.TextReader tr = new StreamReader(absoluteFileName);
                Dictionary<ulong, string> contentIdPairs = new Dictionary<ulong, string>();

                while ((linecount < 80000) && ((line = tr.ReadLine()) != null))
                {
                    linecount++;
                    if (linecount % 1000 == 0) 
                    {
                        // batch a 1000
                        RTPBot.writeDebugLine("Lucene learn {0}", linecount);
                        int numIndexedb = Index(contentIdPairs);
                        Console.WriteLine("Indexed {0} lines.", numIndexedb);
                        Console.WriteLine();
                        contentIdPairs = new Dictionary<ulong, string>();
                    }
                    line = line.Trim();
                    if (line.Length != 0 && line[0] != '#')
                    {
                        contentIdPairs.Add(IncDocId(), line);
                    }
                }
                tr.Close();
                // Indexing:
                int numIndexed = Index(contentIdPairs);
                Console.WriteLine("Indexed {0} lines.", numIndexed);
                Console.WriteLine();

                RTPBot.writeDebugLine("Last Line Mlearn {0}", linecount);
            }
            else
            {
                Console.WriteLine("LUCENE: LoadFileByLines cannot find file '{0}'", filename);
            }

        }


        /// <summary>
        /// This method searches for the search query, then deletes those with a score equal to the top ranked.
        /// </summary>
        /// <param name="query">The search term as a string that the caller wants to search for within the
        /// index as referenced by this object.</param>

        public void DeleteTopScoring(string searchQuery)
        {
            // Searching:
            ulong[] ids;
            string[] results;
            float[] scores;

            int numHits;
            // find it
            Console.WriteLine("Replacing best \"{0}\"...", searchQuery);
            //Search(query, out ids, out results, out scores);
            IndexSearcher indexSearcher = new IndexSearcher(_directory);
            try
            {
                QueryParser queryParser = new QueryParser(_fieldName, _analyzer);
                Query query = queryParser.Parse(searchQuery);
                Hits hits = indexSearcher.Search(query);
                numHits = hits.Length();

                // if we want to do something smarter later
                ids = new ulong[numHits];
                results = new string[numHits];
                scores = new float[numHits];
                for (int i = 0; i < numHits; ++i)
                {
                    float score = hits.Score(i);
                    string text = hits.Doc(i).Get(_fieldName);
                    string idAsText = hits.Doc(i).Get(MyLuceneIndexer.DOC_ID_FIELD_NAME);
                    ids[i] = UInt64.Parse(idAsText);
                    results[i] = text;
                    scores[i] = score;
                }
                if (numHits > 0)
                {
                    //IndexReader indexReader = indexSearcher.GetIndexReader();
                    IndexWriter indexWriter = new IndexWriter(_directory, _analyzer);
                    float topscore = scores[0];
                    for (int i = 0; i < numHits; i++)
                    {
                        if (scores[i] == topscore)
                        {
                            //indexSearcher.GetIndexReader().DeleteDocument(i);
                            //indexReader.DeleteDocuments(new Term( MyLuceneIndexer.DOC_ID_FIELD_NAME, ids[i].ToString () ) );
                            indexWriter.DeleteDocuments(new Term( MyLuceneIndexer.DOC_ID_FIELD_NAME, ids[i].ToString () ) );
                        }
                    }
                    //indexReader.Commit();
                    //indexReader.Close();
                    indexWriter.Commit();
                    indexWriter.Close();
                }

            }
            finally
            {
                indexSearcher.Close();
            }

       }
        /// <summary>
        /// This method searches for the search term passed by the caller.
        /// </summary>
        /// <param name="searchTerm">The search term as a string that the caller wants to search for within the
        /// index as referenced by this object.</param>
        /// <param name="ids">An out parameter that is populated by this method for the caller with docments ids.</param>
        /// <param name="results">An out parameter that is populated by this method for the caller with docments text.</param>
        /// <param name="scores">An out parameter that is populated by this method for the caller with docments scores.</param>
        public void Search(string searchTerm, out ulong[] ids, out string[] results, out float[] scores)
        {
            IndexSearcher indexSearcher = new IndexSearcher(_directory);
            try
            {

                QueryParser queryParser = new QueryParser(_fieldName, _analyzer);
                Query query = queryParser.Parse(searchTerm);
                Hits hits = indexSearcher.Search(query);
                int numHits = hits.Length();

                ids = new ulong[numHits];
                results = new string[numHits];
                scores = new float[numHits];
                
                for (int i = 0; i < numHits; ++i)
                {
                    float score = hits.Score(i);
                    string text = hits.Doc(i).Get(_fieldName);
                    string idAsText = hits.Doc(i).Get(MyLuceneIndexer.DOC_ID_FIELD_NAME);
                    ids[i] = UInt64.Parse(idAsText);
                    results[i] = text;
                    scores[i] = score;
                }

                if (numHits == 0)
                {
                    // Try expansion
                    //QueryParser queryParser = new QueryParser(_fieldName, _analyzer);
                    MultiFieldQueryParser queryParserWN = new MultiFieldQueryParser(
                        new string[] { _fieldName, MyLuceneIndexer.HYPO_FIELD_NAME },
                        _analyzer);
                    string hypo_expand = WordNetExpand(searchTerm,false);
                    Query queryWN = queryParserWN.Parse(hypo_expand);
                    Hits hitsWN = indexSearcher.Search(queryWN);
                    int numHitsWN = hitsWN.Length();

                    ids = new ulong[numHitsWN];
                    results = new string[numHitsWN];
                    scores = new float[numHitsWN];
                    for (int i = 0; i < numHitsWN; ++i)
                    {
                        float score = hitsWN.Score(i);
                        string text = hitsWN.Doc(i).Get(_fieldName);
                        string idAsText = hitsWN.Doc(i).Get(MyLuceneIndexer.DOC_ID_FIELD_NAME);
                        ids[i] = UInt64.Parse(idAsText);
                        results[i] = text;
                        scores[i] = score;
                    }

                }

            }
            finally
            {
                indexSearcher.Close();
            }

        }

        public string WordNetExpand(string inputString,bool queryhook)
        {
            string [] words  = inputString.Split(' ');
            string returnText = inputString+" ";

            int numWords = words.Length;
            // Ok, lets try WordNet
            //WordNetEngine ourWordNetEngine = this.user.bot.wordNetEngine;
            WordNetEngine.POS ourPOS = WordNetEngine.POS.Noun;
            List<WordNetEngine.SynSetRelation> vlist = new List<WordNetEngine.SynSetRelation>(); //[2];
            //vlist.Add(WordNetEngine.SynSetRelation.Hypernym);
            //vlist.Add(WordNetEngine.SynSetRelation.InstanceHypernym);
            vlist.Add(WordNetEngine.SynSetRelation.Hyponym);
            vlist.Add(WordNetEngine.SynSetRelation.InstanceHyponym);



            // retrive synsets
            Set<SynSet> synStartSet = null;
            try { synStartSet = wordNetEngine.GetSynSets("entity", ourPOS); }
            catch (Exception)
            {
                Console.WriteLine("Invalid Start SynSet ID");
                return returnText;
            }

            for (int i = 0; i < numWords; i++)
            {
                string focusWord = words[i];

                Set<SynSet> synDestSet = null;
                try { synDestSet = wordNetEngine.GetSynSets(focusWord, ourPOS); }
                catch (Exception)
                {
                    Console.WriteLine("Invalid Dest SynSet ID");
                    continue;
                }
                int numlinks = 0;
                if (synStartSet.Count > 0)
                {
                    //WordNetEngine.SynSetRelation[] vlist = new WordNetEngine.SynSetRelation[2];
                    //vlist[0] = WordNetEngine.SynSetRelation.Hyponym;
                    //vlist[1] = WordNetEngine.SynSetRelation.InstanceHyponym;
                    foreach (SynSet synSrcSet in synStartSet)
                    {
                        foreach (SynSet synDstSet in synDestSet)
                        {
                            //synSets.Items.Add(synSet);
                            List<SynSet> linkageList = null;

                            linkageList = synSrcSet.GetShortestPathTo(synDstSet, vlist);
                            if ((linkageList != null) && (linkageList.Count > 0))
                            {
                                foreach (SynSet s in linkageList)
                                {
                                    StringBuilder desc = new StringBuilder();
                                    //desc.Append("{");
                                    bool prependComma = false;
                                    foreach (string word in s.Words)
                                    {
                                        desc.Append((prependComma ? ", " : "") + word);
                                        prependComma = true;
                                    }

                                    //desc.Append("}");

                                    //LinkBox.Items.Add(desc.ToString());
                                    returnText = returnText + " " + desc.ToString()+ " ";
                                }
                                //LinkBox.Text = "true";
                                numlinks++;
                                //return;
                            }
                        }
                    }

                }
            }
            if (queryhook)
            {
                if (returnText.Contains("person")) { returnText = returnText + " who"; }
                if (returnText.Contains("imaginary_being")) { returnText = returnText + " who"; }
                if (returnText.Contains("causal_agent")) { returnText = returnText + " who"; }
                
                if (returnText.Contains("entity")) { returnText = returnText + " what"; }
                if (returnText.Contains("location")) { returnText = returnText + " where"; }
                if (returnText.Contains("time_period")) { returnText = returnText + " when"; }
            }
            return returnText.Trim();
        }
    }
}