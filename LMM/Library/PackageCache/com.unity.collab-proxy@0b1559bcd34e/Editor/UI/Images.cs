using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

using Codice.LogWrapper;
using Unity.PlasticSCM.Editor.AssetUtils;

namespace Unity.PlasticSCM.Editor.UI
{
    internal class Images
    {
        internal enum Name
        {
            // Cloud Drive plugin
            IconCloudDriveView,

            None,
            IconCloseButton,
            IconPressedCloseButton,
            IconAddedLocal,
            IconAddedOverlay,
            IconPrivateOverlay,
            IconMovedOverlay,
            IconCheckedOutLocalOverlay,
            IconDeletedLocalOverlay,
            IconDeletedRemote,
            IconDeletedRemoteOverlay,
            IconOutOfSync,
            IconInfoBellNotification,
            IconOutOfSyncOverlay,
            IconMergeLink,
            Ignored,
            IconIgnoredOverlay,
            IconPendingChanges,
            IconConflicted,
            IconConflictedOverlay,
            IconConflictResolvedOverlay,
            IconLockedLocalOverlay,
            IconLockedRemoteOverlay,
            IconRetainedOverlay,
            XLink,
            SecondaryTabClose,
            SecondaryTabCloseHover,
            IconRepository,
            IconPlasticView,
            IconPlasticNotifyIncoming,
            IconPlasticNotifyConflict,
            IconPlasticNotifyPendingChanges,
            IconPlasticNotifyPendingChangesAndIncoming,
            IconPackageUpdateAvailable,
            Loading,
            IconEmptyGravatar,
            Step1,
            Step2,
            Step3,
            StepOk,
            ButtonSsoSignInUnity,
            ButtonSsoSignInEmail,
            ButtonSsoSignInGoogle,
            IconPendingChangesView,
            IconIncomingChangesView,
            IconMergeView,
            IconChangesets,
            IconBranch,
            IconBranches,
            IconBrEx,
            IconCurrentBranch,
            IconUndo,
            Refresh,
            IconInviteUsers,
            IconLock,
            IconLockRetained,
            IconShelve,
            IconClipboard,
            IconLabel,
            IconHistory,
            HideVersionControl,
            GetIncomingChangesIcon,
            ButtonHome,
            IconZoomIn,
            IconZoomOut,
            ArrowCaretLeft,
            ArrowCaretRight,
            IconAdd,
            IconEdit,
            InclusionRule,
            ExclusionRule,
        }

        internal static Texture2D GetImage(Name image)
        {
            return LoadImage(image, false);
        }

        internal static Texture GetFileIcon(string path)
        {
            string relativePath = GetRelativePath.ToApplication(path);

            return GetFileIconFromRelativePath(relativePath);
        }

        internal static Texture GetFileIconFromCmPath(string path)
        {
            return GetFileIconFromRelativePath(
                path.Substring(1).Replace("/",
                Path.DirectorySeparatorChar.ToString()));
        }

        internal static Texture GetDropDownIcon()
        {
            return GetIconFromEditorGUI("icon dropdown");
        }

        internal static Texture GetFolderIcon()
        {
            return GetIconFromEditorGUI("Folder Icon");
        }

        internal static Texture GetFolderOpenedIcon()
        {
            return GetIconFromEditorGUI("FolderOpened Icon");
        }

        internal static Texture GetCloudWorkspaceIcon()
        {
            return GetShelveIcon();
        }

        internal static Texture GetPrivateOverlayIcon()
        {
            if (mPrivateOverlayIcon == null)
                mPrivateOverlayIcon = GetOverlay(Name.IconPrivateOverlay);

            return mPrivateOverlayIcon;
        }

        internal static Texture GetAddedOverlayIcon()
        {
            if (mAddedOverlayIcon == null)
                mAddedOverlayIcon = GetOverlay(Name.IconAddedOverlay);

            return mAddedOverlayIcon;
        }

        internal static Texture GetDeletedLocalOverlayIcon()
        {
            if (mDeletedLocalOverlayIcon == null)
                mDeletedLocalOverlayIcon = GetOverlay(Name.IconDeletedLocalOverlay);

            return mDeletedLocalOverlayIcon;
        }

        internal static Texture GetDeletedRemoteOverlayIcon()
        {
            if (mDeletedRemoteOverlayIcon == null)
                mDeletedRemoteOverlayIcon = GetOverlay(Name.IconDeletedRemoteOverlay);

            return mDeletedRemoteOverlayIcon;
        }

        internal static Texture GetMovedOverlayIcon()
        {
            if (mMovedOverlayIcon == null)
                mMovedOverlayIcon = GetOverlay(Name.IconMovedOverlay);

            return mMovedOverlayIcon;
        }

        internal static Texture GetCheckedOutOverlayIcon()
        {
            if (mCheckedOutOverlayIcon == null)
                mCheckedOutOverlayIcon = GetOverlay(Name.IconCheckedOutLocalOverlay);

            return mCheckedOutOverlayIcon;
        }

        internal static Texture GetOutOfSyncOverlayIcon()
        {
            if (mOutOfSyncOverlayIcon == null)
                mOutOfSyncOverlayIcon = GetOverlay(Name.IconOutOfSyncOverlay);

            return mOutOfSyncOverlayIcon;
        }

        internal static Texture GetConflictedOverlayIcon()
        {
            if (mConflictedOverlayIcon == null)
                mConflictedOverlayIcon = GetOverlay(Name.IconConflictedOverlay);

            return mConflictedOverlayIcon;
        }

        internal static Texture GetConflictResolvedOverlayIcon()
        {
            if (mConflictResolvedOverlayIcon == null)
                mConflictResolvedOverlayIcon = GetOverlay(Name.IconConflictResolvedOverlay);

            return mConflictResolvedOverlayIcon;
        }

        internal static Texture GetLockedLocalOverlayIcon()
        {
            if (mLockedLocalOverlayIcon == null)
                mLockedLocalOverlayIcon = GetOverlay(Name.IconLockedLocalOverlay);

            return mLockedLocalOverlayIcon;
        }

        internal static Texture GetLockedRemoteOverlayIcon()
        {
            if (mLockedRemoteOverlayIcon == null)
                mLockedRemoteOverlayIcon = GetOverlay(Name.IconLockedRemoteOverlay);

            return mLockedRemoteOverlayIcon;
        }

        internal static Texture GetRetainedOverlayIcon()
        {
            if (mRetainedOverlayIcon == null)
                mRetainedOverlayIcon = GetOverlay(Name.IconRetainedOverlay);

            return mRetainedOverlayIcon;
        }

        internal static Texture GetIgnoredOverlayIcon()
        {
            if (mIgnoredOverlayIcon == null)
                mIgnoredOverlayIcon = GetOverlay(Name.IconIgnoredOverlay);

            return mIgnoredOverlayIcon;
        }

        internal static Texture GetWarnIcon()
        {
            return GetIconFromEditorGUI("console.warnicon.sml");
        }

        internal static Texture GetInfoIcon()
        {
            return GetIconFromEditorGUI("console.infoicon.sml");
        }

        internal static Texture GetErrorDialogIcon()
        {
            return GetIconFromEditorGUI("console.erroricon");
        }

        internal static Texture GetWarnDialogIcon()
        {
            return GetIconFromEditorGUI("console.warnicon");
        }

        internal static Texture GetInfoDialogIcon()
        {
            return GetIconFromEditorGUI("console.infoicon");
        }

        internal static Texture GetRefreshIcon()
        {
            if (mRefreshIcon == null)
                mRefreshIcon = GetImage(Name.Refresh);

            return mRefreshIcon;
        }

        internal static Texture GetHideIcon()
        {
            return GetIconFromEditorGUI("scenevis_hidden_hover");
        }

        internal static Texture GetUnhideIcon()
        {
            return GetIconFromEditorGUI("scenevis_visible_hover");
        }

        internal static Texture GetSettingsIcon()
        {
            return GetIconFromEditorGUI("settings");
        }

        internal static Texture GetCloudIcon()
        {
            return GetIconFromEditorGUI("CloudConnect@2x");
        }

        internal static Texture GetCloseIcon()
        {
            if (mCloseIcon == null)
                mCloseIcon = GetImage(Name.SecondaryTabClose);

            return mCloseIcon;
        }

        internal static Texture GetClickedCloseIcon()
        {
            if (mClickedCloseIcon == null)
                mClickedCloseIcon = GetImage(Name.SecondaryTabCloseHover);

            return mClickedCloseIcon;
        }

        internal static Texture GetHoveredCloseIcon()
        {
            if (mHoveredCloseIcon == null)
                mHoveredCloseIcon = GetImage(Name.SecondaryTabCloseHover);

            return mHoveredCloseIcon;
        }

        internal static Texture GetHideVersionControlIcon()
        {
            if (mHideVersionControlIcon == null)
                mHideVersionControlIcon = GetImage(Name.HideVersionControl);

            return mHideVersionControlIcon;
        }

        internal static Texture2D GetUndoIcon()
        {
            if (mUndoIcon == null)
                mUndoIcon = GetImage(Name.IconUndo);

            return mUndoIcon;
        }

        internal static Texture2D GetClipboardIcon()
        {
            if (mClipboardIcon == null)
                mClipboardIcon = GetImage(Name.IconClipboard);

            return mClipboardIcon;
        }

        internal static Texture GetPendingChangesViewIcon()
        {
            if (mPendingChangesViewIcon == null)
                mPendingChangesViewIcon = GetImage(Name.IconPendingChangesView);

            return mPendingChangesViewIcon;
        }

        internal static Texture GetIncomingChangesViewIcon()
        {
            if (mIncomingChangesViewIcon == null)
                mIncomingChangesViewIcon = GetImage(Name.IconIncomingChangesView);

            return mIncomingChangesViewIcon;
        }

        internal static Texture GetMergeViewIcon()
        {
            if (mMergeViewIcon == null)
                mMergeViewIcon = GetImage(Name.IconMergeView);

            return mMergeViewIcon;
        }

        internal static Texture2D GetChangesetsIcon()
        {
            if (mChangesetsIcon == null)
                mChangesetsIcon = GetImage(Name.IconChangesets);

            return mChangesetsIcon;
        }

        internal static Texture2D GetBranchIcon()
        {
            if (mBranchIcon == null)
                mBranchIcon = GetImage(Name.IconBranch);

            return mBranchIcon;
        }

        internal static Texture2D GetBranchesIcon()
        {
            if (mBranchesIcon == null)
                mBranchesIcon = GetImage(Name.IconBranches);

            return mBranchesIcon;
        }

        internal static Texture2D GetBranchExplorerIcon()
        {
            if (mBranchExplorerIcon == null)
                mBranchExplorerIcon = GetImage(Name.IconBrEx);

            return mBranchExplorerIcon;
        }

        internal static Texture2D GetCurrentBranchIcon()
        {
            if (mCurrentBranchIcon == null)
                mCurrentBranchIcon = GetImage(Name.IconCurrentBranch);

            return mCurrentBranchIcon;
        }

        internal static Texture2D GetConflictedIcon()
        {
            if (mConflictedIcon == null)
                mConflictedIcon = GetImage(Name.IconConflicted);

            return mConflictedIcon;
        }

        internal static Texture2D GetPendingChangesIcon()
        {
            if (mPendingChangesIcon == null)
                mPendingChangesIcon = GetImage(Name.IconPendingChanges);

            return mPendingChangesIcon;
        }

        internal static Texture2D GetOutOfSyncIcon()
        {
            if (mOutOfSyncIcon == null)
                mOutOfSyncIcon = GetImage(Name.IconOutOfSync);

            return mOutOfSyncIcon;
        }

        internal static Texture2D GetInfoBellNotificationIcon()
        {
            if (mIncomingNotificationIcon == null)
                mIncomingNotificationIcon = GetImage(Name.IconInfoBellNotification);

            return mIncomingNotificationIcon;
        }

        internal static Texture2D GetPlasticViewIcon()
        {
            if (mPlasticViewIcon == null)
                mPlasticViewIcon = GetImage(Name.IconPlasticView);

            return mPlasticViewIcon;
        }

        internal static Texture2D GetPlasticNotifyIncomingIcon()
        {
            if (mPlasticNotifyIncomingIcon == null)
                mPlasticNotifyIncomingIcon = GetImage(Name.IconPlasticNotifyIncoming);

            return mPlasticNotifyIncomingIcon;
        }

        internal static Texture2D GetPlasticNotifyConflictIcon()
        {
            if (mPlasticNotifyConflictIcon == null)
                mPlasticNotifyConflictIcon = GetImage(Name.IconPlasticNotifyConflict);

            return mPlasticNotifyConflictIcon;
        }

        internal static Texture2D GetPlasticNotifyPendingChangesIcon()
        {
            if (mPlasticNotifyPendingChangesIcon == null)
                mPlasticNotifyPendingChangesIcon = GetImage(Name.IconPlasticNotifyPendingChanges);

            return mPlasticNotifyPendingChangesIcon;
        }

        internal static Texture2D GetPlasticNotifyPendingChangesAndIncomingIcon()
        {
            if (mPlasticNotifyPendingChangesAndIncomingIcon == null)
                mPlasticNotifyPendingChangesAndIncomingIcon = GetImage(Name.IconPlasticNotifyPendingChangesAndIncoming);

            return mPlasticNotifyPendingChangesAndIncomingIcon;
        }

        internal static Texture2D GetCloudDriveViewIcon()
        {
            if (mCloudDriveViewIcon == null)
                mCloudDriveViewIcon = GetImage(Name.IconCloudDriveView);

            return mCloudDriveViewIcon;
        }

        internal static Texture2D GetPackageUpdateAvailableIcon()
        {
            if (mPackageUpdateAvailabledIcon == null)
                mPackageUpdateAvailabledIcon = GetImage(Name.IconPackageUpdateAvailable);

            return mPackageUpdateAvailabledIcon;
        }

        internal static Texture2D GetEmptyGravatar()
        {
            if (mEmptyGravatarIcon == null)
                mEmptyGravatarIcon = Images.GetImage(Images.Name.IconEmptyGravatar);

            return mEmptyGravatarIcon;
        }

        internal static Texture2D GetStepOkIcon()
        {
            if (mStepOkIcon == null)
                mStepOkIcon = Images.GetImage(Images.Name.StepOk);

            return mStepOkIcon;
        }

        internal static Texture2D GetStep1Icon()
        {
            if (mStep1Icon == null)
                mStep1Icon = Images.GetImage(Images.Name.Step1);

            return mStep1Icon;
        }

        internal static Texture2D GetStep2Icon()
        {
            if (mStep2Icon == null)
                mStep2Icon = Images.GetImage(Images.Name.Step2);

            return mStep2Icon;
        }

        internal static Texture2D GetStep3Icon()
        {
            if (mStep3Icon == null)
                mStep3Icon = Images.GetImage(Images.Name.Step3);

            return mStep3Icon;
        }

        internal static Texture2D GetMergeLinkIcon()
        {
            if (mMergeLinkIcon == null)
                mMergeLinkIcon = Images.GetImage(Images.Name.IconMergeLink);

            return mMergeLinkIcon;
        }

        internal static Texture2D GetInclusionRuleIcon()
        {
            if (mInclusionRuleIcon == null)
                mInclusionRuleIcon = Images.GetImage(Images.Name.InclusionRule);

            return mInclusionRuleIcon;
        }

        internal static Texture2D GetExclusionRuleIcon()
        {
            if (mExclusionRuleIcon == null)
                mExclusionRuleIcon = Images.GetImage(Images.Name.ExclusionRule);

            return mExclusionRuleIcon;
        }

        internal static Texture2D GetIconCloseButton()
        {
            if (mIconCloseButton == null)
                mIconCloseButton = Images.GetImage(Images.Name.IconCloseButton);

            return mIconCloseButton;
        }

        internal static Texture2D GetAddedLocalIcon()
        {
            if (mAddedLocalIcon == null)
                mAddedLocalIcon = Images.GetImage(Images.Name.IconAddedLocal);

            return mAddedLocalIcon;
        }

        internal static Texture2D GetDeletedRemoteIcon()
        {
            if (mDeletedRemoteIcon == null)
                mDeletedRemoteIcon = Images.GetImage(Images.Name.IconDeletedRemote);

            return mDeletedRemoteIcon;
        }

        internal static Texture2D GetRepositoryIcon()
        {
            if (mRepositoryIcon == null)
                mRepositoryIcon = Images.GetImage(Images.Name.IconRepository);

            return mRepositoryIcon;
        }

        internal static Texture GetFileIcon()
        {
            if (mFileIcon == null)
                mFileIcon = EditorGUIUtility.FindTexture("DefaultAsset Icon");

            if (mFileIcon == null)
                mFileIcon = GetIconFromAssetPreview(typeof(DefaultAsset));

            if (mFileIcon == null)
                mFileIcon = GetEmptyImage();

            return mFileIcon;
        }

        internal static Texture2D GetLinkUnderlineImage()
        {
            if (mLinkUnderlineImage == null)
            {
                mLinkUnderlineImage = new Texture2D(1, 1);
                mLinkUnderlineImage.hideFlags = HideFlags.HideAndDontSave;
                mLinkUnderlineImage.SetPixel(0, 0, UnityStyles.Colors.Link);
                mLinkUnderlineImage.Apply();
            }

            return mLinkUnderlineImage;
        }

        internal static Texture2D GetInviteUsersIcon()
        {
            if (mInviteUsersIcon == null)
                mInviteUsersIcon = GetImage(Name.IconInviteUsers);

            return mInviteUsersIcon;
        }

        internal static Texture2D GetShelveIcon()
        {
            if (mShelveIcon == null)
                mShelveIcon = GetImage(Name.IconShelve);

            return mShelveIcon;
        }

        internal static Texture2D GetLabelIcon()
        {
            if (mLabelIcon == null)
                mLabelIcon = GetImage(Name.IconLabel);

            return mLabelIcon;
        }

        internal static Texture2D GetHistoryIcon()
        {
            if (mHistoryIcon == null)
                mHistoryIcon = GetImage(Name.IconHistory);

            return mHistoryIcon;
        }

        internal static Texture2D GetLockIcon()
        {
            if (mLockIcon == null)
                mLockIcon = GetImage(Name.IconLock);

            return mLockIcon;
        }

        internal static Texture2D GetLockRetainedIcon()
        {
            if (mLockRetainedIcon == null)
                mLockRetainedIcon = GetImage(Name.IconLockRetained);

            return mLockRetainedIcon;
        }

        internal static Texture2D GetButtonHomeIcon()
        {
            if (mButtonHomeIcon == null)
                mButtonHomeIcon = GetImage(Name.ButtonHome);

            return mButtonHomeIcon;
        }

        internal static Texture2D GetZoomInIcon()
        {
            if (mZoomInIcon == null)
                mZoomInIcon = GetImage(Name.IconZoomIn);

            return mZoomInIcon;
        }

        internal static Texture2D GetZoomOutIcon()
        {
            if (mZoomOutIcon == null)
                mZoomOutIcon = GetImage(Name.IconZoomOut);

            return mZoomOutIcon;
        }

        internal static Texture2D GetArrowCaretLeftIcon()
        {
            if (mArrowCaretLeftIcon == null)
                mArrowCaretLeftIcon = GetImage(Name.ArrowCaretLeft);

            return mArrowCaretLeftIcon;
        }

        internal static Texture2D GetAddIcon()
        {
            if (mAddIcon == null)
                mAddIcon = GetImage(Name.IconAdd);

            return mAddIcon;
        }

        internal static Texture2D GetEditIcon()
        {
            if (mEditIcon == null)
                mEditIcon = GetImage(Name.IconEdit);

            return mEditIcon;
        }

        internal static Texture2D GetArrowCaretRightIcon()
        {
            if (mArrowCaretRightIcon == null)
                mArrowCaretRightIcon = GetImage(Name.ArrowCaretRight);

            return mArrowCaretRightIcon;
        }

        internal static Texture2D GetTreeviewBackgroundTexture()
        {
            if (mTreeviewBackgroundTexture == null)
                mTreeviewBackgroundTexture = GetTextureFromColor(UnityStyles.Colors.TreeViewBackground);

            return mTreeviewBackgroundTexture;
        }

        internal static Texture2D GetToolbarBackgroundTexture()
        {
            if (mToolbarBackground == null)
                mToolbarBackground = GetTextureFromColor(UnityStyles.Colors.ToolbarBackground);

            return mToolbarBackground;
        }

        internal static Texture2D GetColumnsBackgroundTexture()
        {
            if (mColumnsBackgroundTexture == null)
                mColumnsBackgroundTexture = GetTextureFromColor(UnityStyles.Colors.ColumnsBackground);

            return mColumnsBackgroundTexture;
        }

        internal static Texture2D GetInspectorHeaderBackgroundTexture()
        {
            if (mInspectorHeaderBackgroundTexture == null)
                mInspectorHeaderBackgroundTexture = GetTextureFromColor(UnityStyles.Colors.InspectorHeaderBackground);

            return mInspectorHeaderBackgroundTexture;
        }

        internal static Texture2D GetNewTextureFromTexture(Texture2D texture)
        {
            Texture2D result = new Texture2D(texture.width, texture.height, TextureFormat.BGRA32, false);

            // To keep images consistent throughout the plugin,
            // manually set the filter mode
            result.filterMode = FilterMode.Bilinear;
            result.hideFlags = HideFlags.HideAndDontSave;

            return result;
        }

        internal static Texture2D GetNewTextureFromBytes(int width, int height, byte[] bytes)
        {
            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);

            result.LoadImage(bytes);

            // To keep images consistent throughout the plugin,
            // manually set the filter mode
            result.filterMode = FilterMode.Bilinear;
            result.hideFlags = HideFlags.HideAndDontSave;

            return result;
        }

        static Texture2D GetNewTextureFromFile(string path)
        {
            return GetNewTextureFromBytes(1, 1, File.ReadAllBytes(path));
        }

        static Texture2D GetOverlay(Name image)
        {
            return LoadImage(image, true);
        }

        static Texture2D LoadImage(Name image, bool preferFulResImage)
        {
            string imageFileName = image.ToString().ToLower() + ".png";
            string imageFileName2x = image.ToString().ToLower() + "@2x.png";

            string darkImageFileName = string.Format("d_{0}", imageFileName);
            string darkImageFileName2x = string.Format("d_{0}", imageFileName2x);

            string imageFileRelativePath = GetImageFileRelativePath(imageFileName);
            string imageFileRelativePath2x = GetImageFileRelativePath(imageFileName2x);

            string darkImageFileRelativePath = GetImageFileRelativePath(darkImageFileName);
            string darkImageFileRelativePath2x = GetImageFileRelativePath(darkImageFileName2x);

            Texture2D result = null;

            if (EditorGUIUtility.isProSkin)
                result = TryLoadImage(darkImageFileRelativePath, darkImageFileRelativePath2x, preferFulResImage);

            if (result != null)
                return result;

            result = TryLoadImage(imageFileRelativePath, imageFileRelativePath2x, preferFulResImage);

            if (result != null)
                return result;

            mLog.WarnFormat("Image not found: {0}", imageFileName);
            return GetEmptyImage();
        }

        static Texture2D GetEmptyImage()
        {
            if (mEmptyImage == null)
                mEmptyImage = GetTextureFromColor(Color.clear);

            return mEmptyImage;
        }

        static Texture2D GetTextureFromColor(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);

            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;

            return texture;
        }

        static Texture GetFileIconFromRelativePath(string relativePath)
        {
            Texture result = AssetDatabase.GetCachedIcon(relativePath);

            if (result != null)
                return result;

            result = GetFileIconFromKnownExtension(relativePath);

            if (result != null)
                return result;

            return GetFileIcon();
        }

        static Texture GetFileIconFromKnownExtension(string relativePath)
        {
            if (relativePath.EndsWith(UnityConstants.TREEVIEW_META_LABEL))
            {
                relativePath = relativePath.Substring(0,
                    relativePath.Length- UnityConstants.TREEVIEW_META_LABEL.Length);
            }

            Texture result = InternalEditorUtility.FindIconForFile(relativePath);

            if (result != null)
                return result;

            string extension = Path.GetExtension(relativePath).ToLower();

            if (extension == ".anim")
                return GetIconFromAssetPreview(typeof(UnityEngine.AnimationClip));

            if (extension == ".controller" ||
                extension == ".overridecontroller")
                return GetIconFromEditorGUI("AnimatorController Icon");

            return null;
        }

        static Texture2D GetIconFromEditorGUI(string name)
        {
            Texture2D result;

            if (mImagesFromEditorGUICache.TryGetValue(name, out result))
            {
                if (result != null)
                    return result;
                mImagesFromEditorGUICache.Remove(name);
            }

            result = EditorGUIUtility.IconContent(name).image as Texture2D;

            mImagesFromEditorGUICache.Add(name, result);

            return result;
        }

        static Texture2D GetIconFromAssetPreview(System.Type type)
        {
            Texture2D result;

            if (mImagesFromAssetPreviewCache.TryGetValue(type.ToString(), out result))
            {
                if (result != null)
                    return result;
                mImagesFromAssetPreviewCache.Remove(type.ToString());
            }

            result = AssetPreview.GetMiniTypeThumbnail(type);

            mImagesFromAssetPreviewCache.Add(type.ToString(), result);

            return result;
        }

        static string GetImageFileRelativePath(string imageFileName)
        {
            return Path.Combine(
                AssetsPath.GetImagesFolderRelativePath(),
                imageFileName);
        }

        static Texture2D TryLoadImage(
            string imageFileRelativePath, string image2xFilePath, bool preferFulResImage)
        {
            bool isImageAvailable = File.Exists(AssetsPath.GetFullPath.ForPath(imageFileRelativePath));
            bool isImage2XAvailable = File.Exists(AssetsPath.GetFullPath.ForPath(image2xFilePath));

            if ((EditorGUIUtility.pixelsPerPoint > 1f || !isImageAvailable || preferFulResImage) &&
                isImage2XAvailable)
                return LoadTextureFromFile(image2xFilePath);

            if (isImageAvailable)
                return LoadTextureFromFile(imageFileRelativePath);

            return null;
        }

        static Texture2D LoadTextureFromFile(string path)
        {
            Texture2D result;

            if (mImagesByPathCache.TryGetValue(path, out result))
            {
                if (result != null)
                    return result;
                mImagesByPathCache.Remove(path);
            }

            // Don't use AssetDatabase to load as it will
            // pick filtering mode from the asset itself
            // which leads to inconsistent filtering between
            // images.
            result = GetNewTextureFromFile(path);

            mImagesByPathCache.Add(path, result);

            return result;
        }

        static Dictionary<string, Texture2D> mImagesByPathCache =
            new Dictionary<string, Texture2D>();

        static Dictionary<string, Texture2D> mImagesFromEditorGUICache =
            new Dictionary<string, Texture2D>();

        static Dictionary<string, Texture2D> mImagesFromAssetPreviewCache =
            new Dictionary<string, Texture2D>();

        static Texture mFileIcon;

        static Texture mPrivateOverlayIcon;
        static Texture mAddedOverlayIcon;
        static Texture mDeletedLocalOverlayIcon;
        static Texture mDeletedRemoteOverlayIcon;
        static Texture mMovedOverlayIcon;
        static Texture mCheckedOutOverlayIcon;
        static Texture mOutOfSyncOverlayIcon;
        static Texture mConflictedOverlayIcon;
        static Texture mConflictResolvedOverlayIcon;
        static Texture mLockedLocalOverlayIcon;
        static Texture mLockedRemoteOverlayIcon;
        static Texture mRetainedOverlayIcon;
        static Texture mIgnoredOverlayIcon;

        static Texture mRefreshIcon;

        static Texture mCloseIcon;
        static Texture mClickedCloseIcon;
        static Texture mHoveredCloseIcon;
        static Texture mHideVersionControlIcon;

        static Texture2D mLinkUnderlineImage;

        static Texture2D mEmptyImage;

        static Texture2D mTreeviewBackgroundTexture;
        static Texture2D mColumnsBackgroundTexture;
        static Texture2D mToolbarBackground;
        static Texture2D mInspectorHeaderBackgroundTexture;

        static Texture2D mUndoIcon;
        static Texture2D mClipboardIcon;
        static Texture2D mPendingChangesViewIcon;
        static Texture2D mIncomingChangesViewIcon;
        static Texture2D mMergeViewIcon;
        static Texture2D mChangesetsIcon;
        static Texture2D mBranchIcon;
        static Texture2D mBranchesIcon;
        static Texture2D mBranchExplorerIcon;
        static Texture2D mCurrentBranchIcon;
        static Texture2D mPendingChangesIcon;
        static Texture2D mConflictedIcon;
        static Texture2D mOutOfSyncIcon;
        static Texture2D mIncomingNotificationIcon;
        static Texture2D mInviteUsersIcon;
        static Texture2D mShelveIcon;
        static Texture2D mLockIcon;
        static Texture2D mLockRetainedIcon;
        static Texture2D mLabelIcon;
        static Texture2D mHistoryIcon;
        static Texture2D mButtonHomeIcon;
        static Texture2D mZoomInIcon;
        static Texture2D mZoomOutIcon;
        static Texture2D mAddIcon;
        static Texture2D mEditIcon;
        static Texture2D mArrowCaretLeftIcon;
        static Texture2D mArrowCaretRightIcon;

        static Texture2D mPlasticViewIcon;
        static Texture2D mPlasticNotifyIncomingIcon;
        static Texture2D mPlasticNotifyConflictIcon;
        static Texture2D mPlasticNotifyPendingChangesIcon;
        static Texture2D mPlasticNotifyPendingChangesAndIncomingIcon;
        static Texture2D mCloudDriveViewIcon;

        static Texture2D mPackageUpdateAvailabledIcon;

        static Texture2D mEmptyGravatarIcon;
        static Texture2D mStepOkIcon;
        static Texture2D mStep1Icon;
        static Texture2D mStep2Icon;
        static Texture2D mStep3Icon;

        static Texture2D mMergeLinkIcon;
        static Texture2D mAddedLocalIcon;
        static Texture2D mDeletedRemoteIcon;
        static Texture2D mRepositoryIcon;

        static Texture2D mIconCloseButton;
        static Texture2D mInclusionRuleIcon;
        static Texture2D mExclusionRuleIcon;

        static readonly HashSet<string> mAudioExtensions = new HashSet<string> {
            ".wav", ".mp3", ".ogg", ".aiff", ".aif" };
        static readonly HashSet<string> mFontExtensions = new HashSet<string> {
            ".ttf", ".otf" };
        static readonly HashSet<string> mImageExtensions = new HashSet<string> {
            ".png", ".jpg", ".jpeg", ".gif", ".tga", ".bmp", ".tif", ".tiff", ".psd" };
        static readonly HashSet<string> mModelExtensions = new HashSet<string> {
            ".fbx", ".ma", ".mb", ".blend", ".max", ".obj" };

        static readonly ILog mLog = PlasticApp.GetLogger("Images");
    }
}
