using IntuiLab.Kinect.DataUserTracking;
using IntuiLab.Kinect.Enums;
using Microsoft.Kinect;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace IntuiLab.Kinect.GestureRecognizer.Postures
{
    class PostureStayCondition : Condition
    {
        #region Fields

        /// <summary>
        /// Instance of Checker
        /// </summary>
        private readonly Checker m_refChecker;
        
        /// <summary>
        /// Consecutive numbers of frame where the conditions is satisfied
        /// </summary>
        private int m_nIndex;

        /// <summary>
        /// Informe if the gesture is begin or not
        /// </summary>
        private bool m_GestureBegin;

        /// <summary>
        /// Number of frame where the condition is satisfied during the initialization
        /// </summary>
        private int m_frameInitCounter;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="refUser">User Data</param>
        public PostureStayCondition(UserData refUser)
            : base(refUser)
        {
            m_nIndex = 0;
            m_refChecker = new Checker(refUser, PropertiesPluginKinect.Instance.PostureCheckerTolerance);
            m_frameInitCounter = 0;
            m_GestureBegin = false;
        }

        /// <summary>
        /// See description in Condition class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void Check(object sender, NewSkeletonEventArgs e)
        {
            // Calculate vector between Shoulder, elbow and hand right
            Vector3D Vector_ElbowR_ShoulderR = new Vector3D(
                e.m_refSkeletonData.refSkeleton.Joints[JointType.ShoulderRight].Position.X - e.m_refSkeletonData.refSkeleton.Joints[JointType.ElbowRight].Position.X,
                e.m_refSkeletonData.refSkeleton.Joints[JointType.ShoulderRight].Position.Y - e.m_refSkeletonData.refSkeleton.Joints[JointType.ElbowRight].Position.Y,
                e.m_refSkeletonData.refSkeleton.Joints[JointType.ShoulderRight].Position.Z - e.m_refSkeletonData.refSkeleton.Joints[JointType.ElbowRight].Position.Z);

            Vector3D Vector_ElbowR_HandR = new Vector3D(
                e.m_refSkeletonData.refSkeleton.Joints[JointType.WristRight].Position.X - e.m_refSkeletonData.refSkeleton.Joints[JointType.ElbowRight].Position.X,
                e.m_refSkeletonData.refSkeleton.Joints[JointType.WristRight].Position.Y - e.m_refSkeletonData.refSkeleton.Joints[JointType.ElbowRight].Position.Y,
                e.m_refSkeletonData.refSkeleton.Joints[JointType.WristRight].Position.Z - e.m_refSkeletonData.refSkeleton.Joints[JointType.ElbowRight].Position.Z);

            // Calculate vector between Shoulder, elbow and hand left
            Vector3D Vector_ElbowL_ShoulderL = new Vector3D(
                e.m_refSkeletonData.refSkeleton.Joints[JointType.ShoulderLeft].Position.X - e.m_refSkeletonData.refSkeleton.Joints[JointType.ElbowLeft].Position.X,
                e.m_refSkeletonData.refSkeleton.Joints[JointType.ShoulderLeft].Position.Y - e.m_refSkeletonData.refSkeleton.Joints[JointType.ElbowLeft].Position.Y,
                e.m_refSkeletonData.refSkeleton.Joints[JointType.ShoulderLeft].Position.Z - e.m_refSkeletonData.refSkeleton.Joints[JointType.ElbowLeft].Position.Z);

            Vector3D Vector_ElbowL_HandL = new Vector3D(
                e.m_refSkeletonData.refSkeleton.Joints[JointType.WristLeft].Position.X - e.m_refSkeletonData.refSkeleton.Joints[JointType.ElbowLeft].Position.X,
                e.m_refSkeletonData.refSkeleton.Joints[JointType.WristLeft].Position.Y - e.m_refSkeletonData.refSkeleton.Joints[JointType.ElbowLeft].Position.Y,
                e.m_refSkeletonData.refSkeleton.Joints[JointType.WristLeft].Position.Z - e.m_refSkeletonData.refSkeleton.Joints[JointType.ElbowLeft].Position.Z);

            // Calculate angle
            double angleBetweenShoulderL_HandL = Vector3D.AngleBetween(Vector_ElbowL_HandL, Vector_ElbowL_ShoulderL);
            double angleBetweenShoulderR_HandR = Vector3D.AngleBetween(Vector_ElbowR_HandR, Vector_ElbowR_ShoulderR);

            // Relative position between ElbowLeft and WristLeft
            List<EnumKinectDirectionGesture> handLeftToElbowLeftOrientation = m_refChecker.GetRelativePosition(JointType.ElbowLeft, JointType.WristLeft).ToList();

            // Relative position between ElbowRight and WristRight
            List<EnumKinectDirectionGesture> handRightToElbowRightOrientation = m_refChecker.GetRelativePosition(JointType.ElbowRight, JointType.WristRight).ToList();

            // Relative velocity of HandLeft
            double handLeftVelocity = m_refChecker.GetRelativeVelocity(JointType.HipCenter, JointType.WristLeft);

            // Relative velocity of HandRight
            double handRightVelocity = m_refChecker.GetRelativeVelocity(JointType.HipCenter, JointType.WristRight);

            // Speed Condition
            if (handLeftVelocity > PropertiesPluginKinect.Instance.PostureLowerBoundForVelocity || handRightVelocity > PropertiesPluginKinect.Instance.PostureLowerBoundForVelocity)
            {
                Reset();
            }
            // Condition : Angle between HandRight/ElbowRight/ShoulderRight = 90° && HandRight above ElbowRight && HandLeft above ElbowLeft (mirror effect works)
            else if (((angleBetweenShoulderR_HandR <= (PropertiesPluginKinect.Instance.StayAngleShoulderElbowHand + PropertiesPluginKinect.Instance.StayAngleThreshold) && angleBetweenShoulderR_HandR >= (PropertiesPluginKinect.Instance.StayAngleShoulderElbowHand - PropertiesPluginKinect.Instance.StayAngleThreshold))
                    && handLeftToElbowLeftOrientation.Contains(EnumKinectDirectionGesture.KINECT_DIRECTION_DOWNWARD)
                    && handRightToElbowRightOrientation.Contains(EnumKinectDirectionGesture.KINECT_DIRECTION_UPWARD))
                || ((angleBetweenShoulderL_HandL <= (PropertiesPluginKinect.Instance.StayAngleShoulderElbowHand + PropertiesPluginKinect.Instance.StayAngleThreshold) && angleBetweenShoulderL_HandL >= (PropertiesPluginKinect.Instance.StayAngleShoulderElbowHand - PropertiesPluginKinect.Instance.StayAngleThreshold))
                    && handRightToElbowRightOrientation.Contains(EnumKinectDirectionGesture.KINECT_DIRECTION_DOWNWARD)
                    && handLeftToElbowLeftOrientation.Contains(EnumKinectDirectionGesture.KINECT_DIRECTION_UPWARD)))
            {
                // Posture Stay is complete
                if (m_nIndex > PropertiesPluginKinect.Instance.StayLowerBoundForSuccess)
                {
                    // Notify the posture Stay is detected
                    FireSucceeded(this, new SuccessGestureEventArgs
                    {
                        Gesture = EnumGesture.GESTURE_NONE,
                        Posture = EnumPosture.POSTURE_STAY
                    });
                    m_nIndex = 0;
                    m_frameInitCounter = 0;
                }
                else
                {
                    // Posture Stay in initialization
                    if (m_frameInitCounter < PropertiesPluginKinect.Instance.PostureNumberFrameInitialisation)
                    {
                        m_frameInitCounter++;
                        // If initialization is complete
                        if (m_frameInitCounter == PropertiesPluginKinect.Instance.PostureNumberFrameInitialisation)
                        {
                            // Notify the posture Stay is begin
                            RaiseGestureBegining(this, new BeginGestureEventArgs
                            {
                                Gesture = EnumGesture.GESTURE_NONE,
                                Posture = EnumPosture.POSTURE_STAY
                            });
                            m_GestureBegin = true;
                        }
                    }
                    // Step successful, waiting for next
                    else
                    {
                        m_nIndex++;
                        // Notify posture Stay progress
                        RaiseGestureProgressed(this, new ProgressGestureEventArgs
                        {
                            Gesture = EnumGesture.GESTURE_NONE,
                            Posture = EnumPosture.POSTURE_STAY,
                            Percent = ((float)m_nIndex / (float)PropertiesPluginKinect.Instance.StayLowerBoundForSuccess) * 100
                        });
                    }
                }
            }
            // Posture is broken
            else
            {
                Reset();
            }
        }

        /// <summary>
        /// Restart detecting
        /// </summary>
        private void Reset()
        {
            // If posture begin, notify posture is end
            if (m_GestureBegin)
            {
                m_GestureBegin = false;
                RaiseGestureEnded(this, new EndGestureEventArgs
                {
                    Gesture = EnumGesture.GESTURE_NONE,
                    Posture = EnumPosture.POSTURE_STAY
                });
            }
            m_nIndex = 0;
            m_frameInitCounter = 0;
            FireFailed(this, new FailedGestureEventArgs
            {
                refCondition = this
            });
        }
    }
}
