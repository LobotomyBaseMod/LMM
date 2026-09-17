using System.ComponentModel;

namespace Unity.PlasticSCM.Editor.UI
{
    // Internal usage. This isn't a public API.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class UnityConstants
    {
        internal const int LABEL_FONT_SIZE = 12;

        internal const float CANCEL_BUTTON_SIZE = 15f;

        internal const float REGULAR_BUTTON_WIDTH = 60f;
        internal const float EXTRA_LARGE_BUTTON_WIDTH = 130f;

        internal const float SEARCH_FIELD_WIDTH = 350f;
        internal const float DIFF_PANEL_MIN_WIDTH = SEARCH_FIELD_WIDTH / 2f + 8f;

        internal const string TREEVIEW_META_LABEL = " +meta";
        internal const float TREEVIEW_CHECKBOX_SIZE = 17f;
        internal const float TREEVIEW_BASE_INDENT = 16f;
        internal const float TREEVIEW_ROW_WIDTH_OFFSET = 24f;
        internal const float FIRST_COLUMN_WITHOUT_ICON_INDENT = 5f;
        internal const int OVERLAY_STATUS_ICON_SIZE = 16;
        internal const int INSPECTOR_STATUS_ICON_SIZE = 19;

        // Foldouts introduce an extra horizontal space margin of 10 units in front of the labels.
        // Instead of adding an equivalent extra margin on the main area (which QA didn't like)
        // increase its width by the same amount to keep the alignments of the checkboxes.
        internal const int SETTINGS_GUI_WIDTH_MAIN_SECTION = 435;
        internal const int SETTINGS_GUI_WIDTH = 425;

        internal const int STATUS_BAR_HEIGHT = 24;
        internal const int STATUS_BAR_ICON_SIZE = 16;

        internal const int INCOMING_CHANGES_NOTIFICATION_ICON_SIZE = 14;
        internal const int SHELVED_CHANGES_NOTIFICATION_ICON_SIZE = 12;

        internal const float DROPDOWN_ICON_Y_OFFSET = 2f;
        internal const float TREEVIEW_FOLDOUT_Y_OFFSET = 0f;
        internal const float TREEVIEW_ROW_HEIGHT = 24f;
        internal const float TREEVIEW_ICON_SIZE = 20f;
        internal const float TREEVIEW_HEADER_CHECKBOX_Y_OFFSET = 0f;
        internal const float TREEVIEW_CHECKBOX_Y_OFFSET = 0f;
        internal static float DIR_CONFLICT_VALIDATION_WARNING_LABEL_HEIGHT = 21f;

        internal const float INSPECTOR_ACTIONS_BACK_RECTANGLE_TOP_MARGIN = -2f;

        internal const int INSPECTOR_ACTIONS_HEADER_BACK_RECTANGLE_HEIGHT = 7;
        internal const int EMPTY_STATE_HORIZONTAL_PADDING = 3;

        internal const int EMPTY_STATE_FONT_SIZE = 12;
        internal const int EMPTY_STATE_VERTICAL_PADDING = 4;

        internal const int LEFT_MOUSE_BUTTON = 0;
        internal const int RIGHT_MOUSE_BUTTON = 1;

        internal const int UNSORT_COLUMN_ID = -1;

        internal const string UVCS_WINDOW_TITLE = "Unity Version Control";
        internal const string BREX_WINDOW_TITLE = "Branch Explorer";
        internal const string DIFF_WINDOW_TITLE = "Differences";
        internal const string PROJECT_SETTINGS_TAB_PATH = "Project/Version Control/Unity Version Control";
        internal const string PROJECT_SETTINGS_TAB_TITLE = "Unity Version Control Settings";

        internal const float UVCS_WINDOW_MIN_SIZE_WIDTH = 600f;
        internal const float UVCS_WINDOW_MIN_SIZE_HEIGHT = 350f;
        internal const float PENDING_CHANGES_COMMENT_HEIGHT = 55f;

        internal const int ACTIVE_TAB_UNDERLINE_HEIGHT = 1;
        internal const int SPLITTER_INDICATOR_HEIGHT = 1;

        internal const double SEARCH_DELAYED_INPUT_ACTION_INTERVAL = 0.25;
        internal const double SELECTION_DELAYED_INPUT_ACTION_INTERVAL = 0.25;
        internal const double AUTO_REFRESH_CHANGES_DELAYED_INTERVAL = 0.1;
        // Internal usage. This isn't a public API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public const double PLUGIN_DELAYED_INITIALIZE_INTERVAL = 0.25;
        internal const int RECOMMEND_MANUAL_CHECKOUT_DELAYED_FRAMES = 2;
        internal const double REFRESH_ASSET_DATABASE_DELAYED_INTERVAL = 0.25;
        internal const int CHECK_CLOUD_DRIVE_EXE_DELAYED_INTERVAL_MS = 2000;

        internal const double NOTIFICATION_CLEAR_INTERVAL = 8;

        internal const string PENDING_CHANGES_TABLE_SETTINGS_NAME = "{0}_PendingChangesTreeV3_{1}";
        internal const string PENDING_CHANGES_ERRORS_TABLE_SETTINGS_NAME = "{0}_PendingChangesErrorsList{1}";
        internal const string GLUON_INCOMING_CHANGES_TABLE_SETTINGS_NAME = "{0}_GluonIncomingChangesTreeV2_{1}";
        internal const string GLUON_INCOMING_ERRORS_TABLE_SETTINGS_NAME = "{0}_GluonIncomingErrorsListV2_{1}";
        internal const string GLUON_UPDATE_REPORT_TABLE_SETTINGS_NAME = "{0}_GluonUpdateReportListV2_{1}";
        internal const string DEVELOPER_INCOMING_CHANGES_TABLE_SETTINGS_NAME = "{0}_DeveloperIncomingChangesTreeV3_{1}";
        internal const string DEVELOPER_MERGE_TABLE_SETTINGS_NAME = "{0}_DeveloperMergeTreeV3_{1}";
        internal const string DEVELOPER_UPDATE_REPORT_TABLE_SETTINGS_NAME = "{0}_DeveloperUpdateReportListV2_{1}";
        internal const string REPOSITORIES_TABLE_SETTINGS_NAME = "{0}_RepositoriesListV2_{1}";
        internal const string CHANGESETS_TABLE_SETTINGS_NAME = "{0}_ChangesetsListV4_{1}";
        internal const string CHANGESETS_DATE_FILTER_SETTING_NAME = "{0}_ChangesetsDateFilter_{1}";
        internal const string CHANGESETS_SHOW_CHANGES_SETTING_NAME = "{0}_ShowChanges_{1}";
        internal const string HISTORY_TABLE_SETTINGS_NAME = "{0}_HistoryListV3_{1}";
        internal const string BRANCHES_TABLE_SETTINGS_NAME = "{0}_BranchesListV3_{1}";
        internal const string BRANCHES_DATE_FILTER_SETTING_NAME = "{0}_BranchesDateFilter_{1}";
        internal const string LOCKS_TABLE_SETTINGS_NAME = "{0}_LocksListV3_{1}";
        internal const string SHELVES_TABLE_SETTINGS_NAME = "{0}_ShelvesListV2_{1}";
        internal const string SHELVES_OWNER_FILTER_SETTING_NAME = "{0}_ShelvesOwnerFilter_{1}";
        internal const string LABELS_TABLE_SETTINGS_NAME = "{0}_LabelsListV2_{1}";
        internal const string LABELS_DATE_FILTER_SETTING_NAME = "{0}_LabelsDateFilter_{1}";
        internal const string BROWSE_REPOSITORY_TABLE_SETTINGS_NAME = "{0}_BrowseRepositoryList_{1}";

        internal const string PENDING_CHANGES_COMMENTS_SPLITTER_SETTINGS_NAME = "{0}_PendingChangesCommentsSplitter_{1}";
        internal const string CLOUD_WORKSPACES_SPLITTER_SETTINGS_NAME = "{0}_CloudWorkspacesSplitter_{1}";
        internal const string DEVELOPER_UPDATE_REPORT_SPLITTER_SETTINGS_NAME = "{0}_DeveloperUpdateReportSplitter_{1}";
        internal const string GLUON_UPDATE_REPORT_SPLITTER_SETTINGS_NAME = "{0}_GluonUpdateReportSplitter_{1}";
        internal const string PENDING_CHANGES_ERRORS_PANEL_SPLITTER_SETTINGS_NAME = "{0}_PendingChangesErrorsPanelSplitter_{1}";
        internal const string GLUON_INCOMING_ERRORS_PANEL_SPLITTER_SETTINGS_NAME = "{0}_GluonIncomnigErrorsPanelSplitter_{1}";
        internal const string DIFF_SPLITTER_SETTINGS_NAME = "{0}_DiffSplitter_v2{1}";
        internal const string PROPERTIES_SPLITTER_SETTINGS_NAME = "{0}_PropertiesSplitter_{1}";
        internal const string LABELS_SPLITTER_SETTINGS_NAME = "{0}_LabelsSplitter_{1}";
        internal const string GLUON_INCOMING_CHANGES_SPLITTER_SETTINGS_NAME = "{0}_GluonIncomingChangesSplitter_{1}";
        internal const string PENDING_CHANGES_ERRORS_SPLITTER_SETTINGS_NAME = "{0}_PendingChangesErrorsSplitter_{1}";

        internal const string UVCS_PLUGIN_IS_ENABLED_KEY_NAME = "{0}_UVCSPluginIsEnabled";
        internal const string UVCS_PLUGIN_IS_ENABLED_OLD_KEY_NAME = "{0}_PlasticPluginIsEnabled";
        internal const string SHOW_UVCS_TOOLBAR_BUTTON_KEY_NAME = "{0}_ShowUVCSToolbarButton";
        internal const string FIRST_CHECKIN_SUBMITTED = "{0}_FirstCheckinSubmitted";
        internal const string SHOW_NOTIFICATION_KEY_NAME = "ShowNotification";
        internal const string IS_MANUAL_CHECKOUT_ENABLED_KEY_NAME = "{0}_IsManualCheckoutEnabled";
        internal const string IS_MANUAL_CHECKOUT_ALREADY_RECOMMENDED_KEY_NAME = "{0}_IsManualCheckoutAlreadyRecommended";
        internal const string PROJECT_LOADED_COUNTER_KEY_NAME = "{0}_ProjectLoadedCounter";
        internal const string AUTOMATIC_ADD_KEY_NAME = "{0}_AutomaticAdd";
        internal const string PENDING_CHANGES_CI_SUMMARY_KEY_NAME = "CheckInSummary";
        internal const string PENDING_CHANGES_CI_COMMENTS_KEY_NAME = "CheckInComments";
        internal const string PENDING_CHANGES_UNCHECKED_ITEMS_KEY_NAME = "PendingChangesUnchecked";

        internal const float BROWSE_REPOSITORY_PANEL_MIN_WIDTH = SEARCH_FIELD_WIDTH / 2f + 8f;

        internal const float TOOLBAR_ICON_BUTTON_WIDTH = 26f;
        internal const float TOOLBAR_DATE_FILTER_COMBO_WIDTH = 105f;

        internal const float SCROLLBAR_WIDTH = 13f;

        internal static class ChangesetsColumns
        {
            internal const float CHANGESET_NUMBER_WIDTH = 80f;
            internal const float CHANGESET_NUMBER_MIN_WIDTH = 50f;
            internal const float CREATION_DATE_WIDTH = 150f;
            internal const float CREATION_DATE_MIN_WIDTH = 100f;
            internal const float CREATED_BY_WIDTH = 200f;
            internal const float CREATED_BY_MIN_WIDTH = 110f;
            internal const float COMMENT_WIDTH = 300f;
            internal const float COMMENT_MIN_WIDTH = 100f;
            internal const float BRANCH_WIDTH = 160f;
            internal const float BRANCH_MIN_WIDTH = 90f;
            internal const float REPOSITORY_WIDTH = 210f;
            internal const float REPOSITORY_MIN_WIDTH = 90f;
            internal const float GUID_WIDTH = 270f;
            internal const float GUID_MIN_WIDTH = 100f;
        }

        internal static class BranchesColumns
        {
            internal const float BRANCHES_NAME_WIDTH = 180f;
            internal const float BRANCHES_NAME_MIN_WIDTH = 70f;
            internal const float CREATION_DATE_WIDTH = 80f;
            internal const float CREATION_DATE_MIN_WIDTH = 60f;
            internal const float CREATEDBY_WIDTH = 200f;
            internal const float CREATEDBY_MIN_WIDTH = 110f;
            internal const float COMMENT_WIDTH = 300f;
            internal const float COMMENT_MIN_WIDTH = 100f;
            internal const float REPOSITORY_WIDTH = 180f;
            internal const float REPOSITORY_MIN_WIDTH = 90f;
        }

        internal static class LocksColumns
        {
            internal const float PATH_WIDTH = 400f;
            internal const float PATH_MIN_WIDTH = 200f;
            internal const float LOCK_TYPE_WIDTH = 100f;
            internal const float LOCK_TYPE_MIN_WIDTH = 60f;
            internal const float MODIFICATION_DATE_WIDTH = 120f;
            internal const float MODIFICATION_DATE_MIN_WIDTH = 60f;
            internal const float OWNER_WIDTH = 220f;
            internal const float OWNER_MIN_WIDTH = 110f;
            internal const float BRANCH_NAME_WIDTH = 180f;
            internal const float BRANCH_NAME_MIN_WIDTH = 90f;
            internal const float DESTINATION_BRANCH_NAME_WIDTH = 180f;
            internal const float DESTINATION_BRANCH_NAME_MIN_WIDTH = 90f;
        }

        internal static class ShelvesColumns
        {
            internal const float SHELVES_NAME_WIDTH = 80f;
            internal const float SHELVES_NAME_MIN_WIDTH = 50f;
            internal const float CREATION_DATE_WIDTH = 150f;
            internal const float CREATION_DATE_MIN_WIDTH = 100f;
            internal const float CREATEDBY_WIDTH = 200f;
            internal const float CREATEDBY_MIN_WIDTH = 110f;
            internal const float COMMENT_WIDTH = 300f;
            internal const float COMMENT_MIN_WIDTH = 100f;
            internal const float REPOSITORY_WIDTH = 180f;
            internal const float REPOSITORY_MIN_WIDTH = 90f;
        }

        internal static class CloudDrive
        {
            internal const string WINDOW_TITLE = "Unity Cloud Drive";
            internal const string ERRORS_DIALOG_TREE_SETTINGS_NAME = "{0}_CloudDriveErrorsDialog{1}";
            internal const string ERRORS_DIALOG_SPLITTER_SETTINGS_NAME = "{0}_CloudDriveErrorsDialogSplitter_{1}";
            internal const string COLLABORATORS_TABLE_SETTINGS_NAME = "{0}_CollaboratorsList_{1}";
        }

        internal static class LabelsColumns
        {
            internal const float LABELS_NAME_WIDTH = 160f;
            internal const float LABELS_NAME_MIN_WIDTH = 80f;
            internal const float CREATION_DATE_WIDTH = 150f;
            internal const float CREATION_DATE_MIN_WIDTH = 100f;
            internal const float CREATEDBY_WIDTH = 200f;
            internal const float CREATEDBY_MIN_WIDTH = 110f;
            internal const float COMMENT_WIDTH = 300f;
            internal const float COMMENT_MIN_WIDTH = 100f;
            internal const float REPOSITORY_WIDTH = 180f;
            internal const float REPOSITORY_MIN_WIDTH = 90f;
            internal const float CHANGESET_NUMBER_WIDTH = 80f;
            internal const float CHANGESET_NUMBER_MIN_WIDTH = 50f;
            internal const float BRANCH_WIDTH = 180f;
            internal const float BRANCH_MIN_WIDTH = 70f;
        }

        internal static class BrowseRepositoryColumns
        {
            internal const float ITEM_WIDTH = 300f;
            internal const float ITEM_MIN_WIDTH = 160f;
            internal const float SIZE_WIDTH = 80f;
            internal const float SIZE_MIN_WIDTH = 80f;
            internal const float TYPE_WIDTH = 80f;
            internal const float TYPE_MIN_WIDTH = 80f;
            internal const float BRANCH_NAME_WIDTH = 180f;
            internal const float BRANCH_NAME_MIN_WIDTH = 90f;
            internal const float CHANGESET_NUMBER_WIDTH = 80f;
            internal const float CHANGESET_NUMBER_MIN_WIDTH = 50f;
            internal const float CREATEDBY_WIDTH = 200f;
            internal const float CREATEDBY_MIN_WIDTH = 110f;
            internal const float MODIFICATION_DATE_WIDTH = 150f;
            internal const float MODIFICATION_DATE_MIN_WIDTH = 100f;
            internal const float REPOSITORY_WIDTH = 180f;
            internal const float REPOSITORY_MIN_WIDTH = 90f;
        }
    }
}
