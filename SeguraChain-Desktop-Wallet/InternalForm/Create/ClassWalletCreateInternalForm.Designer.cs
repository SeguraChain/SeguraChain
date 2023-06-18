namespace SeguraChain_Desktop_Wallet.InternalForm.Create
{
    partial class ClassWalletCreateInternalForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ClassWalletCreateInternalForm));
            this.tabControlCreateWallet = new System.Windows.Forms.TabControl();
            this.tabPageStep1 = new System.Windows.Forms.TabPage();
            this.labelCreateWalletTitleStepOneText = new System.Windows.Forms.Label();
            this.richTextBoxCreateWalletBaseWordContent = new System.Windows.Forms.RichTextBox();
            this.checkBoxCreateWalletBaseWordWay = new System.Windows.Forms.CheckBox();
            this.labelWalletCreateDescriptionType = new System.Windows.Forms.Label();
            this.buttonWalletCreateNextStepTwoText = new System.Windows.Forms.Button();
            this.richTextBoxWalletCreateTypeDescription = new System.Windows.Forms.RichTextBox();
            this.checkBoxCreateWalletSlowRandomWay = new System.Windows.Forms.CheckBox();
            this.checkBoxCreateWalletFastRandomWay = new System.Windows.Forms.CheckBox();
            this.tabPageStep2 = new System.Windows.Forms.TabPage();
            this.buttonCreateWalletBackToStepOne = new System.Windows.Forms.Button();
            this.labelCreateWalletEncryptionRounds = new System.Windows.Forms.Label();
            this.textBoxCreateWalletTotalEncryptionRounds = new System.Windows.Forms.TextBox();
            this.trackBarCreateWalletEncryptionRounds = new System.Windows.Forms.TrackBar();
            this.buttonWalletCreateNextStepThreeText = new System.Windows.Forms.Button();
            this.checkBoxCreateWalletNoPassword = new System.Windows.Forms.CheckBox();
            this.labelCreateWalletPasswordText = new System.Windows.Forms.Label();
            this.textBoxCreateWalletPassword = new System.Windows.Forms.TextBox();
            this.labelCreateWalletTitleStepTwoText = new System.Windows.Forms.Label();
            this.tabPageStep3 = new System.Windows.Forms.TabPage();
            this.panelCreateWalletInformationResult = new System.Windows.Forms.Panel();
            this.labelCreateWalletPrivateKeyDescription = new System.Windows.Forms.Label();
            this.labelCreateWalletWalletAddress = new System.Windows.Forms.Label();
            this.labelCreateWalletWalletAddressDescription = new System.Windows.Forms.Label();
            this.labelCreateWalletPrivateKey = new System.Windows.Forms.Label();
            this.panelSaveWallet = new System.Windows.Forms.Panel();
            this.labelCreateWalletWalletFileName = new System.Windows.Forms.Label();
            this.textBoxCreateWalletSaveWalletFile = new System.Windows.Forms.TextBox();
            this.buttonCreateWalletSaveWallet = new System.Windows.Forms.Button();
            this.buttonCreateWalletPrintWallet = new System.Windows.Forms.Button();
            this.panelQrCodeWalletAddress = new System.Windows.Forms.Panel();
            this.pictureBoxQrCodeWalletAddress = new System.Windows.Forms.PictureBox();
            this.labelCreateWalletQrCodeWalletAddress = new System.Windows.Forms.Label();
            this.panelQrCodePrivateKey = new System.Windows.Forms.Panel();
            this.pictureBoxQrCodePrivateKey = new System.Windows.Forms.PictureBox();
            this.labelCreateWalletQrCodePrivateKeyText = new System.Windows.Forms.Label();
            this.labelCreateWalletTitleStepThreeText = new System.Windows.Forms.Label();
            this.tabControlCreateWallet.SuspendLayout();
            this.tabPageStep1.SuspendLayout();
            this.tabPageStep2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarCreateWalletEncryptionRounds)).BeginInit();
            this.tabPageStep3.SuspendLayout();
            this.panelCreateWalletInformationResult.SuspendLayout();
            this.panelSaveWallet.SuspendLayout();
            this.panelQrCodeWalletAddress.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxQrCodeWalletAddress)).BeginInit();
            this.panelQrCodePrivateKey.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxQrCodePrivateKey)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControlCreateWallet
            // 
            this.tabControlCreateWallet.Alignment = System.Windows.Forms.TabAlignment.Bottom;
            this.tabControlCreateWallet.Controls.Add(this.tabPageStep1);
            this.tabControlCreateWallet.Controls.Add(this.tabPageStep2);
            this.tabControlCreateWallet.Controls.Add(this.tabPageStep3);
            this.tabControlCreateWallet.ItemSize = new System.Drawing.Size(317, 10);
            this.tabControlCreateWallet.Location = new System.Drawing.Point(-2, 1);
            this.tabControlCreateWallet.Name = "tabControlCreateWallet";
            this.tabControlCreateWallet.SelectedIndex = 0;
            this.tabControlCreateWallet.Size = new System.Drawing.Size(1067, 495);
            this.tabControlCreateWallet.TabIndex = 0;
            this.tabControlCreateWallet.SelectedIndexChanged += new System.EventHandler(this.tabControlCreateWallet_SelectedIndexChanged);
            // 
            // tabPageStep1
            // 
            this.tabPageStep1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(110)))), ((int)(((byte)(149)))), ((int)(((byte)(181)))));
            this.tabPageStep1.Controls.Add(this.labelCreateWalletTitleStepOneText);
            this.tabPageStep1.Controls.Add(this.richTextBoxCreateWalletBaseWordContent);
            this.tabPageStep1.Controls.Add(this.checkBoxCreateWalletBaseWordWay);
            this.tabPageStep1.Controls.Add(this.labelWalletCreateDescriptionType);
            this.tabPageStep1.Controls.Add(this.buttonWalletCreateNextStepTwoText);
            this.tabPageStep1.Controls.Add(this.richTextBoxWalletCreateTypeDescription);
            this.tabPageStep1.Controls.Add(this.checkBoxCreateWalletSlowRandomWay);
            this.tabPageStep1.Controls.Add(this.checkBoxCreateWalletFastRandomWay);
            this.tabPageStep1.Location = new System.Drawing.Point(4, 4);
            this.tabPageStep1.Name = "tabPageStep1";
            this.tabPageStep1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageStep1.Size = new System.Drawing.Size(1059, 477);
            this.tabPageStep1.TabIndex = 0;
            this.tabPageStep1.Text = "TABPAGE_CREATE_WALLET_STEP_ONE_TEXT";
            // 
            // labelCreateWalletTitleStepOneText
            // 
            this.labelCreateWalletTitleStepOneText.AutoSize = true;
            this.labelCreateWalletTitleStepOneText.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelCreateWalletTitleStepOneText.ForeColor = System.Drawing.Color.Ivory;
            this.labelCreateWalletTitleStepOneText.Location = new System.Drawing.Point(269, 12);
            this.labelCreateWalletTitleStepOneText.Name = "labelCreateWalletTitleStepOneText";
            this.labelCreateWalletTitleStepOneText.Size = new System.Drawing.Size(440, 20);
            this.labelCreateWalletTitleStepOneText.TabIndex = 7;
            this.labelCreateWalletTitleStepOneText.Text = "LABEL_CREATE_WALLET_TITLE_STEP_ONE_TEXT";
            // 
            // richTextBoxCreateWalletBaseWordContent
            // 
            this.richTextBoxCreateWalletBaseWordContent.Location = new System.Drawing.Point(31, 242);
            this.richTextBoxCreateWalletBaseWordContent.Name = "richTextBoxCreateWalletBaseWordContent";
            this.richTextBoxCreateWalletBaseWordContent.Size = new System.Drawing.Size(460, 207);
            this.richTextBoxCreateWalletBaseWordContent.TabIndex = 6;
            this.richTextBoxCreateWalletBaseWordContent.Text = "";
            this.richTextBoxCreateWalletBaseWordContent.TextChanged += new System.EventHandler(this.richTextBoxCreateWalletBaseWordContent_TextChanged);
            // 
            // checkBoxCreateWalletBaseWordWay
            // 
            this.checkBoxCreateWalletBaseWordWay.AutoSize = true;
            this.checkBoxCreateWalletBaseWordWay.ForeColor = System.Drawing.Color.Ivory;
            this.checkBoxCreateWalletBaseWordWay.Location = new System.Drawing.Point(31, 217);
            this.checkBoxCreateWalletBaseWordWay.Name = "checkBoxCreateWalletBaseWordWay";
            this.checkBoxCreateWalletBaseWordWay.Size = new System.Drawing.Size(335, 19);
            this.checkBoxCreateWalletBaseWordWay.TabIndex = 5;
            this.checkBoxCreateWalletBaseWordWay.Text = "CHECKBOX_CREATE_BOX_BASE_WORDS_WAY";
            this.checkBoxCreateWalletBaseWordWay.UseVisualStyleBackColor = true;
            this.checkBoxCreateWalletBaseWordWay.CheckedChanged += new System.EventHandler(this.checkBoxCreateWalletBaseWordWay_CheckedChanged);
            // 
            // labelWalletCreateDescriptionType
            // 
            this.labelWalletCreateDescriptionType.AutoSize = true;
            this.labelWalletCreateDescriptionType.ForeColor = System.Drawing.Color.Ivory;
            this.labelWalletCreateDescriptionType.Location = new System.Drawing.Point(555, 109);
            this.labelWalletCreateDescriptionType.Name = "labelWalletCreateDescriptionType";
            this.labelWalletCreateDescriptionType.Size = new System.Drawing.Size(349, 15);
            this.labelWalletCreateDescriptionType.TabIndex = 4;
            this.labelWalletCreateDescriptionType.Text = "LABEL_CREATE_WALLET_WAY_DESCRIPTION_TEXT";
            // 
            // buttonWalletCreateNextStepTwoText
            // 
            this.buttonWalletCreateNextStepTwoText.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonWalletCreateNextStepTwoText.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonWalletCreateNextStepTwoText.Location = new System.Drawing.Point(721, 451);
            this.buttonWalletCreateNextStepTwoText.Name = "buttonWalletCreateNextStepTwoText";
            this.buttonWalletCreateNextStepTwoText.Size = new System.Drawing.Size(286, 30);
            this.buttonWalletCreateNextStepTwoText.TabIndex = 3;
            this.buttonWalletCreateNextStepTwoText.Text = "BUTTON_CREATE_WALLET_NEXT_TEXT";
            this.buttonWalletCreateNextStepTwoText.UseVisualStyleBackColor = false;
            this.buttonWalletCreateNextStepTwoText.Click += new System.EventHandler(this.buttonWalletCreateNextStepTwoText_Click);
            // 
            // richTextBoxWalletCreateTypeDescription
            // 
            this.richTextBoxWalletCreateTypeDescription.Location = new System.Drawing.Point(497, 129);
            this.richTextBoxWalletCreateTypeDescription.Name = "richTextBoxWalletCreateTypeDescription";
            this.richTextBoxWalletCreateTypeDescription.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.richTextBoxWalletCreateTypeDescription.Size = new System.Drawing.Size(510, 320);
            this.richTextBoxWalletCreateTypeDescription.TabIndex = 2;
            this.richTextBoxWalletCreateTypeDescription.Text = "";
            // 
            // checkBoxCreateWalletSlowRandomWay
            // 
            this.checkBoxCreateWalletSlowRandomWay.AutoSize = true;
            this.checkBoxCreateWalletSlowRandomWay.ForeColor = System.Drawing.Color.Ivory;
            this.checkBoxCreateWalletSlowRandomWay.Location = new System.Drawing.Point(31, 171);
            this.checkBoxCreateWalletSlowRandomWay.Name = "checkBoxCreateWalletSlowRandomWay";
            this.checkBoxCreateWalletSlowRandomWay.Size = new System.Drawing.Size(373, 19);
            this.checkBoxCreateWalletSlowRandomWay.TabIndex = 1;
            this.checkBoxCreateWalletSlowRandomWay.Text = "CHECKBOX_CREATE_WALLET_SLOW_RANDOM_WAY";
            this.checkBoxCreateWalletSlowRandomWay.UseVisualStyleBackColor = true;
            this.checkBoxCreateWalletSlowRandomWay.CheckedChanged += new System.EventHandler(this.checkBoxCreateWalletSlowRandomWay_CheckedChanged);
            // 
            // checkBoxCreateWalletFastRandomWay
            // 
            this.checkBoxCreateWalletFastRandomWay.AutoSize = true;
            this.checkBoxCreateWalletFastRandomWay.Checked = true;
            this.checkBoxCreateWalletFastRandomWay.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxCreateWalletFastRandomWay.ForeColor = System.Drawing.Color.Ivory;
            this.checkBoxCreateWalletFastRandomWay.Location = new System.Drawing.Point(31, 129);
            this.checkBoxCreateWalletFastRandomWay.Name = "checkBoxCreateWalletFastRandomWay";
            this.checkBoxCreateWalletFastRandomWay.Size = new System.Drawing.Size(367, 19);
            this.checkBoxCreateWalletFastRandomWay.TabIndex = 0;
            this.checkBoxCreateWalletFastRandomWay.Text = "CHECKBOX_CREATE_WALLET_FAST_RANDOM_WAY";
            this.checkBoxCreateWalletFastRandomWay.UseVisualStyleBackColor = true;
            this.checkBoxCreateWalletFastRandomWay.CheckedChanged += new System.EventHandler(this.checkBoxCreateWalletFastRandomWayText_CheckedChanged);
            // 
            // tabPageStep2
            // 
            this.tabPageStep2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(110)))), ((int)(((byte)(149)))), ((int)(((byte)(181)))));
            this.tabPageStep2.Controls.Add(this.buttonCreateWalletBackToStepOne);
            this.tabPageStep2.Controls.Add(this.labelCreateWalletEncryptionRounds);
            this.tabPageStep2.Controls.Add(this.textBoxCreateWalletTotalEncryptionRounds);
            this.tabPageStep2.Controls.Add(this.trackBarCreateWalletEncryptionRounds);
            this.tabPageStep2.Controls.Add(this.buttonWalletCreateNextStepThreeText);
            this.tabPageStep2.Controls.Add(this.checkBoxCreateWalletNoPassword);
            this.tabPageStep2.Controls.Add(this.labelCreateWalletPasswordText);
            this.tabPageStep2.Controls.Add(this.textBoxCreateWalletPassword);
            this.tabPageStep2.Controls.Add(this.labelCreateWalletTitleStepTwoText);
            this.tabPageStep2.Location = new System.Drawing.Point(4, 4);
            this.tabPageStep2.Name = "tabPageStep2";
            this.tabPageStep2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageStep2.Size = new System.Drawing.Size(1059, 477);
            this.tabPageStep2.TabIndex = 1;
            this.tabPageStep2.Text = "TABPAGE_CREATE_WALLET_STEP_TWO_TEXT";
            // 
            // buttonCreateWalletBackToStepOne
            // 
            this.buttonCreateWalletBackToStepOne.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonCreateWalletBackToStepOne.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonCreateWalletBackToStepOne.Location = new System.Drawing.Point(3, 3);
            this.buttonCreateWalletBackToStepOne.Name = "buttonCreateWalletBackToStepOne";
            this.buttonCreateWalletBackToStepOne.Size = new System.Drawing.Size(287, 32);
            this.buttonCreateWalletBackToStepOne.TabIndex = 11;
            this.buttonCreateWalletBackToStepOne.Text = "BUTTON_CREATE_WALLET_BACK_TEXT";
            this.buttonCreateWalletBackToStepOne.UseVisualStyleBackColor = false;
            this.buttonCreateWalletBackToStepOne.Click += new System.EventHandler(this.buttonCreateWalletBackToStepOne_Click);
            // 
            // labelCreateWalletEncryptionRounds
            // 
            this.labelCreateWalletEncryptionRounds.AutoSize = true;
            this.labelCreateWalletEncryptionRounds.ForeColor = System.Drawing.Color.Ivory;
            this.labelCreateWalletEncryptionRounds.Location = new System.Drawing.Point(15, 272);
            this.labelCreateWalletEncryptionRounds.Name = "labelCreateWalletEncryptionRounds";
            this.labelCreateWalletEncryptionRounds.Size = new System.Drawing.Size(375, 15);
            this.labelCreateWalletEncryptionRounds.TabIndex = 7;
            this.labelCreateWalletEncryptionRounds.Text = "LABEL_CREATE_WALLET_ENCRYPTION_ROUNDS_TEXT";
            // 
            // textBoxCreateWalletTotalEncryptionRounds
            // 
            this.textBoxCreateWalletTotalEncryptionRounds.Location = new System.Drawing.Point(18, 341);
            this.textBoxCreateWalletTotalEncryptionRounds.Name = "textBoxCreateWalletTotalEncryptionRounds";
            this.textBoxCreateWalletTotalEncryptionRounds.ReadOnly = true;
            this.textBoxCreateWalletTotalEncryptionRounds.Size = new System.Drawing.Size(257, 21);
            this.textBoxCreateWalletTotalEncryptionRounds.TabIndex = 6;
            // 
            // trackBarCreateWalletEncryptionRounds
            // 
            this.trackBarCreateWalletEncryptionRounds.Location = new System.Drawing.Point(14, 290);
            this.trackBarCreateWalletEncryptionRounds.Maximum = 30;
            this.trackBarCreateWalletEncryptionRounds.Minimum = 1;
            this.trackBarCreateWalletEncryptionRounds.Name = "trackBarCreateWalletEncryptionRounds";
            this.trackBarCreateWalletEncryptionRounds.Size = new System.Drawing.Size(1035, 45);
            this.trackBarCreateWalletEncryptionRounds.TabIndex = 5;
            this.trackBarCreateWalletEncryptionRounds.Value = 1;
            this.trackBarCreateWalletEncryptionRounds.ValueChanged += new System.EventHandler(this.trackBarCreateWalletEncryptionRounds_ValueChanged);
            // 
            // buttonWalletCreateNextStepThreeText
            // 
            this.buttonWalletCreateNextStepThreeText.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonWalletCreateNextStepThreeText.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonWalletCreateNextStepThreeText.Location = new System.Drawing.Point(749, 447);
            this.buttonWalletCreateNextStepThreeText.Name = "buttonWalletCreateNextStepThreeText";
            this.buttonWalletCreateNextStepThreeText.Size = new System.Drawing.Size(300, 31);
            this.buttonWalletCreateNextStepThreeText.TabIndex = 4;
            this.buttonWalletCreateNextStepThreeText.Text = "BUTTON_CREATE_WALLET_NEXT_TEXT";
            this.buttonWalletCreateNextStepThreeText.UseVisualStyleBackColor = false;
            this.buttonWalletCreateNextStepThreeText.Click += new System.EventHandler(this.buttonWalletCreateNextStepThreeText_Click);
            // 
            // checkBoxCreateWalletNoPassword
            // 
            this.checkBoxCreateWalletNoPassword.AutoSize = true;
            this.checkBoxCreateWalletNoPassword.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            this.checkBoxCreateWalletNoPassword.ForeColor = System.Drawing.Color.Ivory;
            this.checkBoxCreateWalletNoPassword.Location = new System.Drawing.Point(294, 169);
            this.checkBoxCreateWalletNoPassword.Name = "checkBoxCreateWalletNoPassword";
            this.checkBoxCreateWalletNoPassword.Size = new System.Drawing.Size(309, 19);
            this.checkBoxCreateWalletNoPassword.TabIndex = 3;
            this.checkBoxCreateWalletNoPassword.Text = "LABEL_CREATE_WALLET_NO_PASSWORD_TEXT";
            this.checkBoxCreateWalletNoPassword.UseVisualStyleBackColor = true;
            this.checkBoxCreateWalletNoPassword.CheckedChanged += new System.EventHandler(this.checkBoxCreateWalletNoPassword_CheckedChanged);
            // 
            // labelCreateWalletPasswordText
            // 
            this.labelCreateWalletPasswordText.AutoSize = true;
            this.labelCreateWalletPasswordText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelCreateWalletPasswordText.ForeColor = System.Drawing.Color.Ivory;
            this.labelCreateWalletPasswordText.Location = new System.Drawing.Point(15, 150);
            this.labelCreateWalletPasswordText.Name = "labelCreateWalletPasswordText";
            this.labelCreateWalletPasswordText.Size = new System.Drawing.Size(332, 16);
            this.labelCreateWalletPasswordText.TabIndex = 2;
            this.labelCreateWalletPasswordText.Text = "LABEL_CREATE_WALLET_PASSWORD_TEXT";
            // 
            // textBoxCreateWalletPassword
            // 
            this.textBoxCreateWalletPassword.Location = new System.Drawing.Point(18, 169);
            this.textBoxCreateWalletPassword.Name = "textBoxCreateWalletPassword";
            this.textBoxCreateWalletPassword.PasswordChar = '*';
            this.textBoxCreateWalletPassword.Size = new System.Drawing.Size(257, 21);
            this.textBoxCreateWalletPassword.TabIndex = 1;
            this.textBoxCreateWalletPassword.TextChanged += new System.EventHandler(this.textBoxCreateWalletPassword_TextChanged);
            // 
            // labelCreateWalletTitleStepTwoText
            // 
            this.labelCreateWalletTitleStepTwoText.AutoSize = true;
            this.labelCreateWalletTitleStepTwoText.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelCreateWalletTitleStepTwoText.ForeColor = System.Drawing.Color.Ivory;
            this.labelCreateWalletTitleStepTwoText.Location = new System.Drawing.Point(335, 9);
            this.labelCreateWalletTitleStepTwoText.Name = "labelCreateWalletTitleStepTwoText";
            this.labelCreateWalletTitleStepTwoText.Size = new System.Drawing.Size(442, 20);
            this.labelCreateWalletTitleStepTwoText.TabIndex = 0;
            this.labelCreateWalletTitleStepTwoText.Text = "LABEL_CREATE_WALLET_STEP_TWO_TITLE_TEXT";
            // 
            // tabPageStep3
            // 
            this.tabPageStep3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(110)))), ((int)(((byte)(149)))), ((int)(((byte)(181)))));
            this.tabPageStep3.Controls.Add(this.panelCreateWalletInformationResult);
            this.tabPageStep3.Controls.Add(this.panelSaveWallet);
            this.tabPageStep3.Controls.Add(this.panelQrCodeWalletAddress);
            this.tabPageStep3.Controls.Add(this.panelQrCodePrivateKey);
            this.tabPageStep3.Controls.Add(this.labelCreateWalletTitleStepThreeText);
            this.tabPageStep3.Location = new System.Drawing.Point(4, 4);
            this.tabPageStep3.Name = "tabPageStep3";
            this.tabPageStep3.Size = new System.Drawing.Size(1059, 477);
            this.tabPageStep3.TabIndex = 2;
            this.tabPageStep3.Text = "TABPAGE_CREATE_WALLET_STEP_THREE_TEXT";
            // 
            // panelCreateWalletInformationResult
            // 
            this.panelCreateWalletInformationResult.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            this.panelCreateWalletInformationResult.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelCreateWalletInformationResult.Controls.Add(this.labelCreateWalletPrivateKeyDescription);
            this.panelCreateWalletInformationResult.Controls.Add(this.labelCreateWalletWalletAddress);
            this.panelCreateWalletInformationResult.Controls.Add(this.labelCreateWalletWalletAddressDescription);
            this.panelCreateWalletInformationResult.Controls.Add(this.labelCreateWalletPrivateKey);
            this.panelCreateWalletInformationResult.Location = new System.Drawing.Point(4, 337);
            this.panelCreateWalletInformationResult.Name = "panelCreateWalletInformationResult";
            this.panelCreateWalletInformationResult.Size = new System.Drawing.Size(723, 135);
            this.panelCreateWalletInformationResult.TabIndex = 15;
            // 
            // labelCreateWalletPrivateKeyDescription
            // 
            this.labelCreateWalletPrivateKeyDescription.AutoSize = true;
            this.labelCreateWalletPrivateKeyDescription.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelCreateWalletPrivateKeyDescription.Location = new System.Drawing.Point(-2, 17);
            this.labelCreateWalletPrivateKeyDescription.Name = "labelCreateWalletPrivateKeyDescription";
            this.labelCreateWalletPrivateKeyDescription.Size = new System.Drawing.Size(331, 13);
            this.labelCreateWalletPrivateKeyDescription.TabIndex = 12;
            this.labelCreateWalletPrivateKeyDescription.Text = "LABEL_CREATE_WALLET_PRIVATE_KEY_DESCRIPTION_TEXT";
            // 
            // labelCreateWalletWalletAddress
            // 
            this.labelCreateWalletWalletAddress.AutoSize = true;
            this.labelCreateWalletWalletAddress.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelCreateWalletWalletAddress.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.labelCreateWalletWalletAddress.Location = new System.Drawing.Point(-2, 96);
            this.labelCreateWalletWalletAddress.Name = "labelCreateWalletWalletAddress";
            this.labelCreateWalletWalletAddress.Size = new System.Drawing.Size(93, 12);
            this.labelCreateWalletWalletAddress.TabIndex = 9;
            this.labelCreateWalletWalletAddress.Text = "WALLET_ADDRESS";
            // 
            // labelCreateWalletWalletAddressDescription
            // 
            this.labelCreateWalletWalletAddressDescription.AutoSize = true;
            this.labelCreateWalletWalletAddressDescription.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelCreateWalletWalletAddressDescription.Location = new System.Drawing.Point(-2, 83);
            this.labelCreateWalletWalletAddressDescription.Name = "labelCreateWalletWalletAddressDescription";
            this.labelCreateWalletWalletAddressDescription.Size = new System.Drawing.Size(360, 13);
            this.labelCreateWalletWalletAddressDescription.TabIndex = 13;
            this.labelCreateWalletWalletAddressDescription.Text = "LABEL_CREATE_WALLET_WALLET_ADDRESS_DESCRIPTION_TEXT";
            // 
            // labelCreateWalletPrivateKey
            // 
            this.labelCreateWalletPrivateKey.AutoSize = true;
            this.labelCreateWalletPrivateKey.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelCreateWalletPrivateKey.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.labelCreateWalletPrivateKey.Location = new System.Drawing.Point(-1, 36);
            this.labelCreateWalletPrivateKey.Name = "labelCreateWalletPrivateKey";
            this.labelCreateWalletPrivateKey.Size = new System.Drawing.Size(68, 12);
            this.labelCreateWalletPrivateKey.TabIndex = 8;
            this.labelCreateWalletPrivateKey.Text = "PRIVATE_KEY";
            // 
            // panelSaveWallet
            // 
            this.panelSaveWallet.BackColor = System.Drawing.Color.AliceBlue;
            this.panelSaveWallet.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelSaveWallet.Controls.Add(this.labelCreateWalletWalletFileName);
            this.panelSaveWallet.Controls.Add(this.textBoxCreateWalletSaveWalletFile);
            this.panelSaveWallet.Controls.Add(this.buttonCreateWalletSaveWallet);
            this.panelSaveWallet.Controls.Add(this.buttonCreateWalletPrintWallet);
            this.panelSaveWallet.Location = new System.Drawing.Point(726, 337);
            this.panelSaveWallet.Name = "panelSaveWallet";
            this.panelSaveWallet.Size = new System.Drawing.Size(328, 135);
            this.panelSaveWallet.TabIndex = 14;
            // 
            // labelCreateWalletWalletFileName
            // 
            this.labelCreateWalletWalletFileName.AutoSize = true;
            this.labelCreateWalletWalletFileName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelCreateWalletWalletFileName.Location = new System.Drawing.Point(24, 11);
            this.labelCreateWalletWalletFileName.Name = "labelCreateWalletWalletFileName";
            this.labelCreateWalletWalletFileName.Size = new System.Drawing.Size(282, 13);
            this.labelCreateWalletWalletFileName.TabIndex = 2;
            this.labelCreateWalletWalletFileName.Text = "LABEL_CREATE_WALLET_WALLET_FILENAME_TEXT";
            // 
            // textBoxCreateWalletSaveWalletFile
            // 
            this.textBoxCreateWalletSaveWalletFile.Location = new System.Drawing.Point(23, 30);
            this.textBoxCreateWalletSaveWalletFile.Name = "textBoxCreateWalletSaveWalletFile";
            this.textBoxCreateWalletSaveWalletFile.Size = new System.Drawing.Size(285, 21);
            this.textBoxCreateWalletSaveWalletFile.TabIndex = 1;
            // 
            // buttonCreateWalletSaveWallet
            // 
            this.buttonCreateWalletSaveWallet.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonCreateWalletSaveWallet.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonCreateWalletSaveWallet.Location = new System.Drawing.Point(23, 57);
            this.buttonCreateWalletSaveWallet.Name = "buttonCreateWalletSaveWallet";
            this.buttonCreateWalletSaveWallet.Size = new System.Drawing.Size(285, 30);
            this.buttonCreateWalletSaveWallet.TabIndex = 3;
            this.buttonCreateWalletSaveWallet.Text = "BUTTON_CREATE_WALLET_SAVE_TEXT";
            this.buttonCreateWalletSaveWallet.UseVisualStyleBackColor = false;
            this.buttonCreateWalletSaveWallet.Click += new System.EventHandler(this.buttonCreateWalletSaveWallet_Click);
            // 
            // buttonCreateWalletPrintWallet
            // 
            this.buttonCreateWalletPrintWallet.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonCreateWalletPrintWallet.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonCreateWalletPrintWallet.Location = new System.Drawing.Point(23, 93);
            this.buttonCreateWalletPrintWallet.Name = "buttonCreateWalletPrintWallet";
            this.buttonCreateWalletPrintWallet.Size = new System.Drawing.Size(285, 31);
            this.buttonCreateWalletPrintWallet.TabIndex = 11;
            this.buttonCreateWalletPrintWallet.Text = "BUTTON_CREATE_WALLET_PRINT_TEXT";
            this.buttonCreateWalletPrintWallet.UseVisualStyleBackColor = false;
            this.buttonCreateWalletPrintWallet.Click += new System.EventHandler(this.buttonCreateWalletPrintWallet_Click);
            // 
            // panelQrCodeWalletAddress
            // 
            this.panelQrCodeWalletAddress.BackColor = System.Drawing.Color.AliceBlue;
            this.panelQrCodeWalletAddress.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelQrCodeWalletAddress.Controls.Add(this.pictureBoxQrCodeWalletAddress);
            this.panelQrCodeWalletAddress.Controls.Add(this.labelCreateWalletQrCodeWalletAddress);
            this.panelQrCodeWalletAddress.Location = new System.Drawing.Point(533, 51);
            this.panelQrCodeWalletAddress.Name = "panelQrCodeWalletAddress";
            this.panelQrCodeWalletAddress.Size = new System.Drawing.Size(522, 280);
            this.panelQrCodeWalletAddress.TabIndex = 13;
            // 
            // pictureBoxQrCodeWalletAddress
            // 
            this.pictureBoxQrCodeWalletAddress.BackColor = System.Drawing.Color.WhiteSmoke;
            this.pictureBoxQrCodeWalletAddress.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBoxQrCodeWalletAddress.Location = new System.Drawing.Point(166, 43);
            this.pictureBoxQrCodeWalletAddress.Name = "pictureBoxQrCodeWalletAddress";
            this.pictureBoxQrCodeWalletAddress.Size = new System.Drawing.Size(200, 200);
            this.pictureBoxQrCodeWalletAddress.TabIndex = 10;
            this.pictureBoxQrCodeWalletAddress.TabStop = false;
            // 
            // labelCreateWalletQrCodeWalletAddress
            // 
            this.labelCreateWalletQrCodeWalletAddress.AutoSize = true;
            this.labelCreateWalletQrCodeWalletAddress.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelCreateWalletQrCodeWalletAddress.Location = new System.Drawing.Point(114, 18);
            this.labelCreateWalletQrCodeWalletAddress.Name = "labelCreateWalletQrCodeWalletAddress";
            this.labelCreateWalletQrCodeWalletAddress.Size = new System.Drawing.Size(289, 13);
            this.labelCreateWalletQrCodeWalletAddress.TabIndex = 7;
            this.labelCreateWalletQrCodeWalletAddress.Text = "LABEL_CREATE_QR_CODE_WALLET_ADDRESS_TEXT";
            // 
            // panelQrCodePrivateKey
            // 
            this.panelQrCodePrivateKey.BackColor = System.Drawing.Color.AliceBlue;
            this.panelQrCodePrivateKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelQrCodePrivateKey.Controls.Add(this.pictureBoxQrCodePrivateKey);
            this.panelQrCodePrivateKey.Controls.Add(this.labelCreateWalletQrCodePrivateKeyText);
            this.panelQrCodePrivateKey.Location = new System.Drawing.Point(4, 51);
            this.panelQrCodePrivateKey.Name = "panelQrCodePrivateKey";
            this.panelQrCodePrivateKey.Size = new System.Drawing.Size(528, 280);
            this.panelQrCodePrivateKey.TabIndex = 12;
            // 
            // pictureBoxQrCodePrivateKey
            // 
            this.pictureBoxQrCodePrivateKey.BackColor = System.Drawing.Color.WhiteSmoke;
            this.pictureBoxQrCodePrivateKey.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBoxQrCodePrivateKey.Location = new System.Drawing.Point(158, 43);
            this.pictureBoxQrCodePrivateKey.Name = "pictureBoxQrCodePrivateKey";
            this.pictureBoxQrCodePrivateKey.Size = new System.Drawing.Size(200, 200);
            this.pictureBoxQrCodePrivateKey.TabIndex = 9;
            this.pictureBoxQrCodePrivateKey.TabStop = false;
            // 
            // labelCreateWalletQrCodePrivateKeyText
            // 
            this.labelCreateWalletQrCodePrivateKeyText.AutoSize = true;
            this.labelCreateWalletQrCodePrivateKeyText.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelCreateWalletQrCodePrivateKeyText.Location = new System.Drawing.Point(105, 18);
            this.labelCreateWalletQrCodePrivateKeyText.Name = "labelCreateWalletQrCodePrivateKeyText";
            this.labelCreateWalletQrCodePrivateKeyText.Size = new System.Drawing.Size(310, 13);
            this.labelCreateWalletQrCodePrivateKeyText.TabIndex = 5;
            this.labelCreateWalletQrCodePrivateKeyText.Text = "LABEL_CREATE_WALLET_QR_CODE_PRIVATE_KEY_TEXT";
            // 
            // labelCreateWalletTitleStepThreeText
            // 
            this.labelCreateWalletTitleStepThreeText.AutoSize = true;
            this.labelCreateWalletTitleStepThreeText.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelCreateWalletTitleStepThreeText.ForeColor = System.Drawing.Color.Ivory;
            this.labelCreateWalletTitleStepThreeText.Location = new System.Drawing.Point(264, 12);
            this.labelCreateWalletTitleStepThreeText.Name = "labelCreateWalletTitleStepThreeText";
            this.labelCreateWalletTitleStepThreeText.Size = new System.Drawing.Size(463, 20);
            this.labelCreateWalletTitleStepThreeText.TabIndex = 0;
            this.labelCreateWalletTitleStepThreeText.Text = "LABEL_CREATE_WALLET_TITLE_STEP_THREE_TEXT";
            // 
            // ClassWalletCreateInternalForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(90)))), ((int)(((byte)(120)))));
            this.ClientSize = new System.Drawing.Size(1063, 497);
            this.Controls.Add(this.tabControlCreateWallet);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.Name = "ClassWalletCreateInternalForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FORM_CREATE_WALLET_TITLE_TEXT";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ClassWalletCreateInternalForm_FormClosing);
            this.Load += new System.EventHandler(this.ClassWalletCreateInternalForm_Load);
            this.tabControlCreateWallet.ResumeLayout(false);
            this.tabPageStep1.ResumeLayout(false);
            this.tabPageStep1.PerformLayout();
            this.tabPageStep2.ResumeLayout(false);
            this.tabPageStep2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarCreateWalletEncryptionRounds)).EndInit();
            this.tabPageStep3.ResumeLayout(false);
            this.tabPageStep3.PerformLayout();
            this.panelCreateWalletInformationResult.ResumeLayout(false);
            this.panelCreateWalletInformationResult.PerformLayout();
            this.panelSaveWallet.ResumeLayout(false);
            this.panelSaveWallet.PerformLayout();
            this.panelQrCodeWalletAddress.ResumeLayout(false);
            this.panelQrCodeWalletAddress.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxQrCodeWalletAddress)).EndInit();
            this.panelQrCodePrivateKey.ResumeLayout(false);
            this.panelQrCodePrivateKey.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxQrCodePrivateKey)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControlCreateWallet;
        private System.Windows.Forms.TabPage tabPageStep1;
        private System.Windows.Forms.TabPage tabPageStep2;
        private System.Windows.Forms.TabPage tabPageStep3;
        private System.Windows.Forms.Label labelWalletCreateDescriptionType;
        private System.Windows.Forms.Button buttonWalletCreateNextStepTwoText;
        private System.Windows.Forms.RichTextBox richTextBoxWalletCreateTypeDescription;
        private System.Windows.Forms.CheckBox checkBoxCreateWalletSlowRandomWay;
        private System.Windows.Forms.CheckBox checkBoxCreateWalletFastRandomWay;
        private System.Windows.Forms.RichTextBox richTextBoxCreateWalletBaseWordContent;
        private System.Windows.Forms.CheckBox checkBoxCreateWalletBaseWordWay;
        private System.Windows.Forms.Label labelCreateWalletEncryptionRounds;
        private System.Windows.Forms.TextBox textBoxCreateWalletTotalEncryptionRounds;
        private System.Windows.Forms.TrackBar trackBarCreateWalletEncryptionRounds;
        private System.Windows.Forms.Button buttonWalletCreateNextStepThreeText;
        private System.Windows.Forms.CheckBox checkBoxCreateWalletNoPassword;
        private System.Windows.Forms.Label labelCreateWalletPasswordText;
        private System.Windows.Forms.TextBox textBoxCreateWalletPassword;
        private System.Windows.Forms.Label labelCreateWalletTitleStepTwoText;
        private System.Windows.Forms.Label labelCreateWalletQrCodeWalletAddress;
        private System.Windows.Forms.Label labelCreateWalletQrCodePrivateKeyText;
        private System.Windows.Forms.Button buttonCreateWalletSaveWallet;
        private System.Windows.Forms.Label labelCreateWalletWalletFileName;
        private System.Windows.Forms.TextBox textBoxCreateWalletSaveWalletFile;
        private System.Windows.Forms.Label labelCreateWalletTitleStepThreeText;
        private System.Windows.Forms.Label labelCreateWalletWalletAddress;
        private System.Windows.Forms.Label labelCreateWalletPrivateKey;
        private System.Windows.Forms.Label labelCreateWalletTitleStepOneText;
        private System.Windows.Forms.Button buttonCreateWalletPrintWallet;
        private System.Windows.Forms.Button buttonCreateWalletBackToStepOne;
        private System.Windows.Forms.Panel panelQrCodePrivateKey;
        private System.Windows.Forms.Panel panelQrCodeWalletAddress;
        private System.Windows.Forms.Panel panelSaveWallet;
        private System.Windows.Forms.PictureBox pictureBoxQrCodeWalletAddress;
        private System.Windows.Forms.PictureBox pictureBoxQrCodePrivateKey;
        private System.Windows.Forms.Label labelCreateWalletWalletAddressDescription;
        private System.Windows.Forms.Label labelCreateWalletPrivateKeyDescription;
        private System.Windows.Forms.Panel panelCreateWalletInformationResult;
    }
}