using ColossalFramework;
using ICities;
using System;
using UnityEngine;
using ColossalFramework.UI;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
namespace NoTreeDirt
{
	public class ModLoader : LoadingExtensionBase
	{
        private static List<uint> m_MarkedForUpdate;
        internal const string DTMilli = "MM/dd/yyyy hh:mm:ss.fff tt";
        internal Stopwatch dbgTimer = new Stopwatch();
        internal bool m_IsCoroutineActive = false; //testing 
        internal bool m_abortCoroutine = false; //testing
        private bool m_CoroutineTest = false;   //testing
        private static UIView gameview; //testing
        public ModLoader()
		{

		}

        //can be removed was here for testing trying to do something before data got loaded.
        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
        }

        public override void OnLevelUnloading()
        {
            try
            {
                m_abortCoroutine = true;
                if (gameview != null)
                {
                    gameview.StopCoroutine(UpdateExistingTrees());
                }
                if (m_MarkedForUpdate != null)
                {
                    m_MarkedForUpdate.Clear();
                }
                base.OnLevelUnloading();
            }
            catch (Exception ex)
            { Helper.dbgLog("Exception: ", ex, true); }
        }

		public override void OnLevelLoaded(LoadMode mode)
		{
            try
            {
                if (Mod.isEnabled && mode != LoadMode.LoadAsset && mode != LoadMode.NewAsset)
                {
                    //First we need to gather data about the trees because
                    //info.m_createruining is actually only true on the first load
                    //before we change the assets, this saves us from processing
                    //uncessessary true\false and updates upon re-loads.
                    //What I mean by that is if you loop though the info.m_createruining's
                    //on each tree instance after you change the <TreeInfo>'s you will
                    //always get 'false'...I guess because they are but they've already been
                    //rendered first as true. So we need to figure out who's got to be updated
                    //for that before we actually update the asset's core TreeInfo setting.
                    //
                    //only side effect is we loadup our m_MarkedForUpdate list even
                    //if the option is not enabled... but overall it's worth it for
                    //that use case to take the extra couple milliseconds. adding
                    //and if() to change that also adds tiny per hit so it's almost a wash.
                    int retval = 0;
                    if (Mod.config.UpdateTreeAssets)
                    {

                        //go gather the ones we need for later, and while it's at it
                        //mark their ruining false.
                        uint tcount = countRuiningTrues();
                        retval = GatherRuiningData();

                        //ok now go change all 'assets' that are tree, custom or not.
                        SetupAllTreeAssets();
                    }

                    //Do they have the option set to actually update existing trees on-map-load?
                    //Even if they do, is there anything to actually do?
                    if (Mod.config.UpdateTreeAssets && Mod.config.UpdateResetTrees && m_MarkedForUpdate.Count > 0)
                    {
                        //yea they do! and yeah there is!

                        // are we using couroutine route?
                        if (!m_CoroutineTest)
                        {
                            //no? Good cause it is the suck for our purposes!

                            //go call the expensive operation of updating all the trees
                            //that we need too, the list only includes ones that were
                            //a)created b)have info objects c)are not hidden d)were previously
                            //marked as having ruining=true ie.. not bushes or other assets.
                            //where the prefab was made with ruining = false.
                            ProcessTreesForUpdate();
                        }
                        else
                        {
                            //testing - only ever called during test builds where you change the trigger var up top.
                            Helper.dbgLog("Using coroutine version. prepare for framerate death ;) .");
                            gameview = UIView.GetAView(); 
                            gameview.StartCoroutine(UpdateExistingTrees());
                        }
                    }

                }
            }
            catch(Exception ex)
            {Helper.dbgLog("Exception: ",ex,true);}
		}


        private void SetupAllTreeAssets()
        {
            try
            {
                uint i = 0;
                int ticount = 0;
                ticount = PrefabCollection<TreeInfo>.PrefabCount();
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Tree prefab count to process is: " + ticount.ToString()); }
                uint tcount = 0;
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { dbgTimer.Start(); }

                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
                { 
                    tcount = countRuiningTrues();
                    Helper.dbgLog(string.Format("truecount before  {0}", tcount.ToString())); 
                }

                if (ticount > 0)
                {
                    TreeInfo ti;
                    for (i = 0; i < ticount; i++)
                    {
                        ti = PrefabCollection<TreeInfo>.GetPrefab(i);
                        if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog(string.Format("changing {0} {1}", i.ToString(), ti.name)); }
                        ti.m_createRuining = false;
                    }

                    if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
                    {
                        dbgTimer.Stop();
                        Helper.dbgLog(string.Format("It took {0} milliseconds to complete SetupAllTreeAssets", dbgTimer.ElapsedMilliseconds.ToString()));
                        dbgTimer.Reset();
                    }
                    Helper.dbgLog("All tree assets updated.");
                }
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
                { 
                    tcount = countRuiningTrues();
                    Helper.dbgLog(string.Format("truecount after  {0}", tcount.ToString())); 
                }
            }
            catch(Exception ex)
            {
                Helper.dbgLog("Exception: ", ex,true); 
            }
        }


        /// <summary>
        /// Function to loop through all tress count't the ones where ruining is fall
        /// we call this from various debug routines just see what the number is
        /// in particular before and after actions.
        /// </summary>
        /// <returns> number of trees where ruining was set</returns>
        private uint countRuiningTrues()
        {
            TreeInstance[] mBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
            uint tcount = 0;
            for(uint i=0;i < mBuffer.Length;i++)
            {

                if ((mBuffer[i].m_flags & (ushort)TreeInstance.Flags.Created) == (ushort)TreeInstance.Flags.Created &&
                    (mBuffer[i].m_flags & (ushort)TreeInstance.Flags.Hidden) == 0 && mBuffer[i].Info != null)
                { 
                    if(mBuffer[i].Info.m_createRuining)
                    {
                        tcount++;
                    }
                }
            }
            return tcount;
        }

        /// <summary>
        /// Function loops though all trees. If they are created, not hidden, and have info objects
        /// it will check thier m_createRuining setting, if true, it will add them to the global
        /// list to perhaps be used later, we can not set runing=false here it will screw up our
        /// results as these info's ref\link back to the asset's core tree-info themselves, change one and you
        /// change it for all of the same tree type before we've even gotten to it.
        /// 
        /// Note: Example 316k tree map; 120k were actual tree's the rest bushes.
        /// priviously we would have ended up running terrain update on 316k vs 120k.
        /// difference is ~22 real seconds saved no process ones that were already false. 
        /// </summary>
        /// <returns>Number of items added to m_MarkedForUpdate list</returns>
        private int GatherRuiningData()
        {
            uint i = 0;
            try
            {
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { dbgTimer.Start(); }

                TreeInstance[] mBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
                //slightly more effecient to set it larger then we need then to keep auto growing
                //after every few adds. We'll never exceed mbuffer.length.
                m_MarkedForUpdate = new List<uint>(mBuffer.Length);
 
                for (i = 0; i < mBuffer.Length; i++)
                {
                    if ((mBuffer[i].m_flags & (ushort)TreeInstance.Flags.Created) == (ushort)TreeInstance.Flags.Created &&
                        (mBuffer[i].m_flags & (ushort)TreeInstance.Flags.Hidden) == 0 && mBuffer[i].Info != null)
                    {
                        if (mBuffer[i].Info.m_createRuining)
                        {
                            m_MarkedForUpdate.Add(i);
                        }
                    }
                }
                //Now in theory we could just not do this... but the use case exists where
                //this will run but ProcessTreesForUpdate (which clears and releases this)
                //will not and in that case we're using up to 4mb of ram for this thing
                //let's not do that and at least trim it down to the size we are using
                //which is probably 1/2 or a 1/4th of that at the cost of a millisecond.
                m_MarkedForUpdate.TrimExcess(); //let's stop using up ram we don't need.

                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
                {
                    dbgTimer.Stop();
                    Helper.dbgLog(string.Format("It took {0} milliseconds to gather the ruining data. {1} trees added.", dbgTimer.ElapsedMilliseconds.ToString(),m_MarkedForUpdate.Count.ToString()));
                    dbgTimer.Reset();
                }
            }
            catch (Exception ex)
            { 
                Helper.dbgLog("Exception, unable to finish gathering data, last index accessed was " + i.ToString(), ex, true); 
            }
            return m_MarkedForUpdate.Count;
        }


        //old... we don't need this anymore because no need to set to false.
        //used to use it to set everything to false. 
        private void SetupExistingTrees()
        { 
            try
            {
                uint tcount = countRuiningTrues();
                TreeInstance[] mBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { dbgTimer.Start(); }
                uint donecount = 0;

                //start at one as zero == game created dummy tree object
                for (uint i = 0; i < (int)mBuffer.Length; i++)
                {
                    if ((mBuffer[i].m_flags & (ushort)TreeInstance.Flags.Created) == (ushort)TreeInstance.Flags.Created &&
                        (mBuffer[i].m_flags & (ushort)TreeInstance.Flags.Hidden) == 0 &&
                        mBuffer[i].Info != null)
                    {
                        mBuffer[i].Info.m_createRuining = false;
                        donecount++;
                    }
                }
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
                {
                    dbgTimer.Stop();
                    Helper.dbgLog(string.Format("It took {0} milliseconds to complete SetupExistingTrees", dbgTimer.ElapsedMilliseconds.ToString()));
                    dbgTimer.Reset();
                }
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Changed " + donecount.ToString() + " existing trees"); }
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("tcount: " + tcount.ToString()); }

            }
            catch(Exception ex)
            {
                Helper.dbgLog("Exception: " ,ex,true); 
            }
        }


        /// <summary>
        /// Function to actually loop though m_MarkedForUpdates and call terrainmodify.updatearea
        /// for each one.
        /// 
        /// note: This is the actual one in use in release.
        /// </summary>
        private void ProcessTreesForUpdate()
        {
            int i = 0;
            try
            {
                if (m_MarkedForUpdate == null || m_MarkedForUpdate.Count == 0)
                {
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("No trees were marked as needing updating."); }
                    return;
                }
                if (Mod.DEBUG_LOG_ON) { dbgTimer.Start(); }
                float minX, minZ, maxX, maxZ;
                TreeInstance[] mBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
                uint j = 0;
                for (i = 0; i < m_MarkedForUpdate.Count; i++)
                {
                    j = m_MarkedForUpdate[i];

                    //now... technically we shouldn't need this if() wrapper
                    //because if they're in the list they were already checked.
                    //and we're calling this function millisecond after....
                    //but better safe then exception-ing ;) 
                    if ((mBuffer[j].m_flags & (ushort)TreeInstance.Flags.Created) == (ushort)TreeInstance.Flags.Created &&
                        (mBuffer[j].m_flags & (ushort)TreeInstance.Flags.Hidden) == 0 &&
                        mBuffer[j].Info != null)
                    {
                    
                        mBuffer[j].Info.m_createRuining = false; //probably already is 90% of time.
                        minX = mBuffer[j].Position.x - 4f;
                        minZ = mBuffer[j].Position.z - 4f;
                        maxX = mBuffer[j].Position.x + 4f;
                        maxZ = mBuffer[j].Position.z + 4f;
                        TerrainModify.UpdateArea(minX, minZ, maxX, maxZ, false, true, false);
                    }
                }
                if (Mod.DEBUG_LOG_ON)
                {
                    dbgTimer.Stop();
                    Helper.dbgLog(string.Format("It took {0} milliseconds to process {1} tree area updates.", dbgTimer.ElapsedMilliseconds.ToString(), m_MarkedForUpdate.Count.ToString()));
                    dbgTimer.Reset();
                }
                Helper.dbgLog(m_MarkedForUpdate.Count.ToString() + " trees were updated.");
            }
            catch (Exception ex)
            {
                Helper.dbgLog(string.Format("Exception, we may not have completed all trees, processed {0} of {1}.",i.ToString(),m_MarkedForUpdate.Count.ToString()), ex, true);
            }
            m_MarkedForUpdate.Clear(); //if all went well let's clean up.
            //deref so this guy can be garabage collected no need to hang onto it new map load re-creates it anyway.
            //in general there is no need to do this but since we could be hanging on to as much
            //as 4mb of ram up to 1million tress x int's = 4bytes we should clear and release him.
            //costs us 1 miliseconds...maybe 2 or even 3 on very slow machine; so what in comparison to the 'seconds' we just burned.
            m_MarkedForUpdate.TrimExcess();
            m_MarkedForUpdate = null;
        }


        /// <summary>
        /// Testing\playing around function
        /// </summary>
        private void originalSetupTest()
        {
            try
            {
                if (Mod.DEBUG_LOG_ON) { dbgTimer.Start(); }
                float minX, minZ, maxX, maxZ;
                TreeInstance[] mBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
                for(uint i = 1;i < mBuffer.Length ;i++)
                {
                    if ((mBuffer[i].m_flags & (ushort)TreeInstance.Flags.Created) == (ushort)TreeInstance.Flags.Created &&
                        mBuffer[i].Info != null)
                    {
                        minX = mBuffer[i].Position.x - 4f;
                        minZ = mBuffer[i].Position.z - 4f;
                        maxX = mBuffer[i].Position.x + 4f;
                        maxZ = mBuffer[i].Position.z + 4f;
                        //TreeManager TM = Singleton<TreeManager>.instance;
                        //object[] tmpobj = new object[] { i, mBuffer[i], false };
                        //typeof(TreeManager).GetMethod("InitializeTree",System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(TM,tmpobj);
                        //Vector3 a = mBuffer[i].Position + new Vector3(-4.5f, 0f, -4.5f);
                        //Vector3 b = mBuffer[i].Position + new Vector3(-4.5f, 0f, 4.5f);
                        //Vector3 c = mBuffer[i].Position + new Vector3(4.5f, 0f, 4.5f);
                        //Vector3 d = mBuffer[i].Position + new Vector3(4.5f, 0f, -4.5f);
                        //TerrainModify.Edges edges = TerrainModify.Edges.All;
                        //TerrainModify.Heights heights = TerrainModify.Heights.None;
                        //TerrainModify.Surface surface = TerrainModify.Surface.Ruined; 
                        //TerrainModify.ApplyQuad(a, b, c, d, edges, heights, surface);

                        //Singleton<TreeManager>.instance.UpdateTreeRenderer(i,true);
                        TerrainModify.UpdateArea(minX, minZ, maxX, maxZ, false, true, false);
                    }
                }
                if (Mod.DEBUG_LOG_ON)
                { 
                    dbgTimer.Stop();
                    Helper.dbgLog(string.Format("It took {0} milliseconds to complete originalsetup to run", dbgTimer.ElapsedMilliseconds.ToString()));
                    dbgTimer.Reset();
                }
            }
            catch(Exception ex)
            {
                Helper.dbgLog("exception in orginal version",ex,true);
            }
        
        }



        /// <summary>
        /// Coroutine improved version - still takes too fucking long
        /// so I don't think it's worth it.
        /// 
        /// Note: unless the game is pause it also can throw some rare errors
        /// that don't stop it from running or this from working, but whats really odd 
        /// in test map with 300 trees 150kish actual trees vs bushes
        /// it would do this 3 times if not pause, worse though even though
        /// we have error handling out the ass they bubble up to the user
        /// because they get caught by C\O.... odd though that when I don't
        /// seperate things out into DoChuck() but leave them in core of the
        /// coroutine they don't bubble up to the user and we catch them.
        /// wtf?
        /// 
        /// 11/7/2015 - This is dead code (does not get called)
        ///  in the release product it's here for experimentation.
        /// </summary>
        /// <returns></returns>
        private IEnumerator UpdateExistingTrees()
        {
            if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Coroutine has started: " + DateTime.Now.ToString(DTMilli ));}
            m_IsCoroutineActive = true;
            if (m_MarkedForUpdate != null && m_MarkedForUpdate.Count > 0)
            {
                uint i = 0;
                uint p = 0;
                uint donecount =0;
                //float minX, minZ, maxX, maxZ;
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog(string.Format("There are {0} trees to update.",m_MarkedForUpdate.Count.ToString())); }
                TreeInstance[] mBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
                long[] perfarray = new long[(m_MarkedForUpdate.Count / 64) + 1];
                while (i < m_MarkedForUpdate.Count)
                {
                    if (m_abortCoroutine) { Helper.dbgLog("aborting due to shutdown."); yield break; }
                    try
                    {
                        if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { dbgTimer.Start(); }
                        // Sperated this out into DoChuck() mainly for modularity.
                        // thinking of pulling it back in here though if end up using this method
                        // have to rework though to catch the errors that have no rhyme or reason to them.
                        //uint j = 0;
                        //for (j = 0; j < 32; j++)
                        //{
                        //    if (i+j >= m_MarkedForUpdate.Count)
                        //    {
                        //        Helper.dbgLog("i+j >= m_Marked count ;breaking due to overage");
                        //        break; 
                        //    }

                        //    //are we marked, and if so is the tree still created, cause it could be minutes later.
                        //    uint tmpi = m_MarkedForUpdate[(int)(i+j)];
                        //    Helper.dbgLog(string.Format("i + j = {0} tmpi={1}",(i+j).ToString(),tmpi.ToString()));
                        //    if ((mBuffer[tmpi].m_flags & (ushort)TreeInstance.Flags.Created) == (ushort)TreeInstance.Flags.Created &&
                        //        mBuffer[tmpi].Info != null)
                        //    {
                        //        //this area will actually update the ground for existing tree otherwise they don't get updated
                        //        //till the next terrain update...ie if you build something near them etc.
                        //        minX = mBuffer[tmpi].Position.x - 4f;
                        //        minZ = mBuffer[tmpi].Position.z - 4f;
                        //        maxX = mBuffer[tmpi].Position.x + 4f;
                        //        maxZ = mBuffer[tmpi].Position.z + 4f;
                        //        //Helper.dbgLog("calling terrain modified  " + minX.ToString("n3") + " , " + minZ.ToString("n3") + " , " + maxX.ToString("n3") + " , " + maxZ.ToString("n3") + " mflag=" + mBuffer[tmpi].m_flags.ToString() );
                        //        TerrainModify.UpdateArea(minX, minZ, maxX, maxZ, false, true, false);
                        //        //m_MarkedForUpdate[(int)(i+j)] = 0;
                        //        donecount++;
                        //    }
                        //}
                        uint j = 64; //chunk size 
                        donecount += DoChunk(i, j);
                        if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog(string.Format("Completed chunck {0} to {1} time: {2}", i.ToString(), (i + j).ToString(), DateTime.Now.ToString(DTMilli))); }
                        i = i + j;
                 
                        if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL >= 2)
                        {
                            dbgTimer.Stop();
                            perfarray[p] = dbgTimer.ElapsedTicks;
                            p++;
                            Helper.dbgLog(string.Format("It took {0} ticks to complete last chunk. donecount:{1}", dbgTimer.ElapsedTicks.ToString(), donecount.ToString()));
                            dbgTimer.Reset();
                        }
                    }
                    catch (Exception ex)
                    {
                        Helper.dbgLog("Exception idx: " + i.ToString(),ex,true );
                        m_IsCoroutineActive = false;
                        yield break;
                    }
                    if (i >= m_MarkedForUpdate.Count)
                    {
                        Helper.dbgLog("Exiting coroutine index exceeds count, we are done.");
                        break;
                    }
                    yield return null;
                }
                if (Mod.DEBUG_LOG_ON)
                { Helper.dbgLog(string.Format("Updater co-routine has completed. {0} trees were updated. time:{1}", donecount.ToString(),DateTime.Now.ToString(DTMilli))); }
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL >= 2)
                {
                    long lTotal = 0;
                    for (p = 0; p < perfarray.Length; p++)
                    {
                        lTotal += perfarray[p];
                    }
                    Helper.dbgLog(string.Format("average chunk took {0}",(lTotal / perfarray.Length).ToString("n3")));
                }
                m_IsCoroutineActive = false;
                yield break;
            }
            else
            {
                Helper.dbgLog("Coroutine started but MarkedForUpdates was null, aborted. time: " + DateTime.Now.ToString(DTMilli));
                m_IsCoroutineActive = false;
                yield break;
            }
        }

        /// <summary>
        /// Process a given chuck, do an terrain update for each.
        /// </summary>
        /// <param name="istart">Start index in m_MarkedForUpdate</param>
        /// <param name="ichunksize">number of indexes to process</param>
        /// <returns>uint - the number successfully processed without error</returns>
        private uint DoChunk(uint istart,uint ichunksize)
        {
            uint donecount = 0;
            try 
            {
                TreeManager TM = Singleton<TreeManager>.instance;
                uint j = 0;
                float minX, minZ, maxX, maxZ;
                for (j = 0; j < ichunksize; j++)
                {
                    if (m_abortCoroutine) { Helper.dbgLog("aborting due to shutdown."); break; }

                    if (istart + j < m_MarkedForUpdate.Count)
                    {
                        int current_i = (int)(istart + j);
                        uint idx = m_MarkedForUpdate[current_i];
                        try
                        {
                            //Helper.dbgLog(string.Format("current_i = {0} idxvalue={1}", current_i.ToString(), idx.ToString()));
                            if ((TM.m_trees.m_buffer[idx].m_flags & (ushort)TreeInstance.Flags.Created) == (ushort)TreeInstance.Flags.Created)
                            {
                                //this area will actually update the ground for existing tree otherwise they don't get updated
                                //till the next terrain update...ie if you build something near them etc.
                                minX = TM.m_trees.m_buffer[idx].Position.x - 4f;
                                minZ = TM.m_trees.m_buffer[idx].Position.z - 4f;
                                maxX = TM.m_trees.m_buffer[idx].Position.x + 4f;
                                maxZ = TM.m_trees.m_buffer[idx].Position.z + 4f;
                                //Helper.dbgLog("calling terrain modified  " + minX.ToString("n3") + " , " + minZ.ToString("n3") + " , " + maxX.ToString("n3") + " , " + maxZ.ToString("n3") + " mflag=" + TM.m_trees.m_buffer[idx].m_flags.ToString());
                                TerrainModify.UpdateArea(minX, minZ, maxX, maxZ, false, true, false);
                                //m_MarkedForUpdate[(int)(i + j)] = 0;
                                donecount++;
                            }
                        }
                        catch (IndexOutOfRangeException ex)
                        {
                            Helper.dbgLog(string.Format("IdxOutOfRange: died in current_i:{0} idx{1} ... continuing", current_i.ToString(), idx.ToString()), ex, true);
                            Helper.dbgLog(string.Format("died in current_i:{0} idx{1} ... continuing", TM.m_trees.m_buffer[idx].Info.name, TM.m_trees.m_buffer[idx].m_flags.ToString()), ex, true);
                        }
                        catch (Exception ex)
                        {
                            Helper.dbgLog(string.Format("died in current_i:{0} idx{1} ... continuing", current_i.ToString(), idx.ToString()), ex, true);
                            Helper.dbgLog(string.Format("died in current_i:{0} idx{1} ... continuing", TM.m_trees.m_buffer[idx].Info.name, TM.m_trees.m_buffer[idx].m_flags.ToString()), ex, true);
                        }
                    }
                    else 
                    {
                        if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("istart + j >= m_markedforUpdate.count"); }
                    }
                }
            }
            catch (Exception ex)
            { Helper.dbgLog("",ex,true);}
            return donecount;
        }
    }
}