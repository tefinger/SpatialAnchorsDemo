﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.SpatialAnchors;
using SpatialAnchors.Core.Interfaces;
using SpatialAnchors.Core;
using System.Collections.Concurrent;
using SpatialAnchors.IOS.Models;
using ARKit;

using SceneKit;
using CoreGraphics;
using Foundation;
using OpenTK;
using System.Diagnostics;
using UIKit;



namespace SpatialAnchors.iOS.Services
{
    /// <summary>
    /// Spatial Anchors for ios
    /// </summary>
    public class SpatialAnchorsService : ISpatialAnchorsService
    {
        private ARSCNView sceneView;
        //private ModelRenderable modelRenderable;                
        //private Context context;
        private CloudSpatialAnchorSession spatialAnchorsSession;
        //private TrackingState lastTrackingState = TrackingState.Stopped;
        //private TrackingFailureReason lastTrackingFailureReason = TrackingFailureReason.None;
        //private readonly object progressLock = new object();
        private bool enoughDataForSaving;
        private readonly ConcurrentDictionary<string, AnchorModel> anchorVisuals = new ConcurrentDictionary<string, AnchorModel>();
                

        /// </inheritdoc>
        public SpatialAnchorsMode Mode { get; protected set; }


        /// </inheritdoc>
        public SpatialAnchorStatus Status { get; protected set; }


        /// </inheritdoc>
        public EventHandler<Models.Anchor> SaveAnchor { get; set; }


        /// </inheritdoc>
        public EventHandler<string> ShowMessage { get; set; }


        /// </inheritdoc>
        public void StartSession(object context, object arScene)
        {          
            this.sceneView = arScene as ARSCNView;
            this.spatialAnchorsSession = new CloudSpatialAnchorSession();
            
            this.spatialAnchorsSession.Configuration.AccountKey = Constants.SpatialAnchorsAccountKey;
            this.spatialAnchorsSession.Configuration.AccountId = Constants.SpatialAnchorsAccountId;
            this.spatialAnchorsSession.Session = this.sceneView.Session;
            this.spatialAnchorsSession.AnchorLocated += this.OnAnchorLocated;
            this.spatialAnchorsSession.LocateAnchorsCompleted += this.OnLocateAnchorsCompleted;
            this.spatialAnchorsSession.SessionUpdated += this.SessionUpdated;
            //this.spatialAnchorsSession.TokenRequired += SpatialAnchorsSession_TokenRequired;
            this.spatialAnchorsSession.Error += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.ErrorMessage))
                {
                    ShowMessage(this, e.ErrorMessage);
                }
            };

            this.spatialAnchorsSession.Start();           
            this.Status  = SpatialAnchorStatus.Iddle;
        }



        //private async void SpatialAnchorsSession_TokenRequired(object sender, TokenRequiredEventArgs e)
        //{
        //   // var token = await this.spatialAnchorsSession.GetAccessTokenWithAccountKeyAsync(Constants.SpatialAnchorsAccountKey).GetAsync();     
        //}


        /// <summary>
        /// 
        /// </summary>        
        private void SessionUpdated(object sender, SessionUpdatedEventArgs e)
        {
            if (this.Mode == SpatialAnchorsMode.AddAnchors && this.Status == SpatialAnchorStatus.Scanning)
            {
                SessionStatus status = e.Status;
                this.enoughDataForSaving = status.RecommendedForCreateProgress >= 1.0;

                if (this.enoughDataForSaving)
                {
                    this.Status = SpatialAnchorStatus.Saving; 
                }
                    
                    //{

                    //    if (this.Status == SpatialAnchorStatus.Scanning && !enoughDataForSaving)
                    //    {                     
                    //        return;
                    //    }
                    //    if (enoughDataForSaving)
                    //    {
                    //        if (this.Status == SpatialAnchorStatus.Saving) return;
                    //        if (this.anchorVisuals.TryGetValue(string.Empty, out AnchorModel model))
                    //        {
                    //            try
                    //            {
                    //                this.Status = SpatialAnchorStatus.Saving;
                    //                var cloudAnchor = new CloudSpatialAnchor
                    //                {
                    //                    LocalAnchor = model.LocalAnchor.Anchor
                    //                };
                    //                model.CloudAnchor = cloudAnchor;
                    //                var now = new Date();
                    //                var calendar = Calendar.Instance;
                    //                calendar.Time = now;
                    //                calendar.Add(CalendarField.Date, 7);
                    //                var oneWeekFromNow = calendar.Time;
                    //                cloudAnchor.Expiration = oneWeekFromNow;
                    //                Task.Run(async () =>
                    //                {
                    //                    try
                    //                    {                                      
                    //                        var result = await this.spatialAnchorsSession.CreateAnchorAsync(cloudAnchor).GetAsync();
                    //                        var anchorId = cloudAnchor.Identifier;
                    //                        this.anchorVisuals[anchorId] = model;
                    //                        this.anchorVisuals.TryRemove(string.Empty, out _);
                    //                        SaveAnchor(this, new SpatialAnchors.Models.Anchor { AnchorId = cloudAnchor.Identifier });                                        
                    //                        this.Status = SpatialAnchorStatus.Iddle;
                    //                    }
                    //                    catch (Exception ex)
                    //                    {
                    //                        ShowMessage(this, "ErrorSavingAnchor");
                    //                    }
                    //                });
                    //            }
                    //            catch
                    //            {

                    //            }
                    //        }
                    //    }
                    //}
                }
        }


        /// </inheritdoc>
        public void StopSession()
        {            
            if (this.Status != SpatialAnchorStatus.NotStarted)
            {
                StopLocatingAnchors();
                this.spatialAnchorsSession.Stop();
                this.Status = SpatialAnchorStatus.NotStarted;
                foreach (var visual in this.anchorVisuals.Values)
                {
                    if (visual.Node != null)
                    {
                        visual.Node.RemoveFromParentNode();
                    }
                }
                this.anchorVisuals.Clear();
            }
        }


        /// </inheritdoc>
        public void ProcessFrame(object frame)
        {
            var arFrame = frame as ARFrame;
            if (frame == null) return;
            if (this.spatialAnchorsSession == null) return;
            this.spatialAnchorsSession.ProcessFrame(arFrame);

            if (this.Mode == SpatialAnchorsMode.AddAnchors 
                && this.Status == SpatialAnchorStatus.Saving
                && this.enoughDataForSaving)
            {
                ///his.source.CreateCloudAnchor();

                //this.currentlyPlacingAnchor = false;
                //this.UpdateMainStatusTitle("Cloud Anchor being saved...");

                //var cloudAnchor = new CloudSpatialAnchor
                //{
                //    LocalAnchor = this.localAnchor
                //};

                //// In this sample app we delete the cloud anchor explicitly, but you can also set it to expire automatically
                //DateTime now = DateTime.Today;
                //DateTimeOffset oneWeekFromNow = now.AddDays(7);
//                this.cloudAnchor.Expiration = oneWeekFromNow;

                Task.Run(async () =>
                {
                    try
                    {
                        //await this.spatialAnchorsSession.CreateAnchorAsync(cloudAnchor);
                        //var anchorId = cloudAnchor.Identifier;
                        //this.anchorVisuals[anchorId] = model;
                        //this.anchorVisuals.TryRemove(string.Empty, out _);
                        //SaveAnchor(this, new Models.Anchor { AnchorId = cloudAnchor.Identifier });
                        //this.Status = SpatialAnchorStatus.Iddle;
                    }                                                     
                    catch (Exception ex)
                    {
                        ShowMessage(this, "ErrorSavingAnchor");
                    }
                });
            }
        }

        
        /// <summary>
        /// Called when it finish to locating anchors
        /// </summary>       
        private void OnLocateAnchorsCompleted(object sender, LocateAnchorsCompletedEventArgs e)
        {
            StopLocatingAnchors();          
        }


        /// </inheritdoc>  
        public void StartLocatingAnchors(string[] anchors)
        {
            StopLocatingAnchors();
            var criteria = new AnchorLocateCriteria
            {
                Identifiers = anchors
            };
            this.spatialAnchorsSession.CreateWatcher(criteria);
        }


        /// <summary>
        /// Stops searching for anchors, only one watcher is active
        /// </summary>
        private void StopLocatingAnchors()
        {
            var watcher = this.spatialAnchorsSession.GetActiveWatchers().FirstOrDefault();
            watcher?.Stop();
        }


        /// <summary>
        /// Called when an anchor is found
        /// </summary>        
        private void OnAnchorLocated(object sender, AnchorLocatedEventArgs e)
        {
            if (e.Status == LocateAnchorStatus.AlreadyTracked)
            {
                // Nothing to do since we've already rendered any anchors we've located.
            }
            else if (e.Status == LocateAnchorStatus.Located)
            {
                var anchor = e.Anchor;                
                var model = new AnchorModel
                {
                    CloudAnchor = anchor,                    
                    LocalAnchor = anchor.LocalAnchor
                };
                this.anchorVisuals[anchor.Identifier] = model;
                this.sceneView.Session.AddAnchor(anchor.LocalAnchor);

                var modelNode = LoadMOdel();                
                modelNode.Position = model.LocalAnchor.Transform.ToPosition();               
                this.sceneView.Scene.RootNode.AddChildNode(modelNode);            
            }
        }


        /// <summary>
        /// Load the Andy Android Model
        /// </summary>        
        private SCNNode LoadMOdel()
        {
            try
            {
                var modelName = "art.scnassets/andy.usdz";                    
                var scene = SCNScene.FromFile(modelName);
                var modelNode = scene.RootNode.ChildNodes[0];
             modelNode.Scale = new SCNVector3(0.8f, 0.8f, 0.8f);
                return modelNode;
            }
            catch (Exception ex)
            {
                ShowMessage(this, "Error loading model");
            }
            return null;
        }



        /// <summary>
        /// Loads the 3D models to use        
        /// </summary>
        public void LoadModels()
        {            
        }
    }

    public static class NMatrix4Extensions
    {
        /// <summary>
        /// Converts a transform to a position.
        /// </summary>
        public static SCNVector3 ToPosition(this NMatrix4 transform)
        {
            return new SCNVector3(transform.M14, transform.M24, transform.M34);
        }
    }
}