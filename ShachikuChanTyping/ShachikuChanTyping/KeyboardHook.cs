using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

//�p�N���c�Q�l��: http://hongliang.seesaa.net/article/7539988.html
namespace ShachikuChanTyping 
{
	///<summary>�L�[�{�[�h�����삳�ꂽ�Ƃ��Ɏ��s����郁�\�b�h��\���C�x���g�n���h���B</summary>
	public delegate void KeyboardHookedEventHandler(object sender, KeyboardHookedEventArgs e);
	///<summary>KeyboardHooked�C�x���g�̃f�[�^��񋟂���B</summary>
	public class KeyboardHookedEventArgs : CancelEventArgs {
		///<summary>�V�����C���X�^���X���쐬����B</summary>
		internal KeyboardHookedEventArgs(KeyboardMessage message, ref KeyboardState state) {
			this.message = message;
			this.state = state;
		}
		private KeyboardMessage message;
		private KeyboardState state;
		///<summary>�L�[�{�[�h�������ꂽ�������ꂽ����\���l���擾����B</summary>
		public KeyboardUpDown UpDown {
			get {
				return (message == KeyboardMessage.KeyDown || message == KeyboardMessage.SysKeyDown) ?
					KeyboardUpDown.Down : KeyboardUpDown.Up;
			}
		}
		///<summary>���삳�ꂽ�L�[�̉��z�L�[�R�[�h��\���l���擾����B</summary>
		public Keys KeyCode {get {return state.KeyCode;}}
		///<summary>���삳�ꂽ�L�[�̃X�L�����R�[�h��\���l���擾����B</summary>
		public int ScanCode {get {return state.ScanCode;}}
		///<summary>���삳�ꂽ�L�[���e���L�[�Ȃǂ̊g���L�[���ǂ�����\���l���擾����B</summary>
		public bool IsExtendedKey {get {return state.Flag.IsExtended;}}
		///<summary>ALT�L�[��������Ă��邩�ǂ�����\���l���擾����B</summary>
		public bool AltDown {get {return state.Flag.AltDown;}}
	}
	///<summary>�L�[�{�[�h��������Ă��邩������Ă��邩��\���B</summary>
	public enum KeyboardUpDown {
		///<summary>�L�[�͉�����Ă���B</summary>
		Down,
		///<summary>�L�[�͕�����Ă���B</summary>
		Up,
	}

	///<summary>���b�Z�[�W�R�[�h��\���B</summary>
	internal enum KeyboardMessage {
		///<summary>�L�[�������ꂽ�B</summary>
		KeyDown    = 0x100,
		///<summary>�L�[�������ꂽ�B</summary>
		KeyUp      = 0x101,
		///<summary>�V�X�e���L�[�������ꂽ�B</summary>
		SysKeyDown = 0x104,
		///<summary>�V�X�e���L�[�������ꂽ�B</summary>
		SysKeyUp   = 0x105,
	}
	///<summary>�L�[�{�[�h�̏�Ԃ�\���B</summary>
	internal struct KeyboardState {
		///<summary>���z�L�[�R�[�h�B</summary>
		public Keys KeyCode;
		///<summary>�X�L�����R�[�h�B</summary>
		public int ScanCode;
		///<summary>�e�����t���O�B</summary>
		public KeyboardStateFlag Flag;
		///<summary>���̃��b�Z�[�W������ꂽ�Ƃ��̎��ԁB</summary>
		public int Time;
		///<summary>���b�Z�[�W�Ɋ֘A�Â���ꂽ�g�����B</summary>
		public IntPtr ExtraInfo;
	}
	///<summary>�L�[�{�[�h�̏�Ԃ�⑫����B</summary>
	internal struct KeyboardStateFlag {
		private int flag;
		private bool IsFlagging(int value) {
			return (flag & value) != 0;
		}
		private void Flag(bool value, int digit) {
			flag = value ? (flag | digit) : (flag & ~digit);
		}
		///<summary>�L�[���e���L�[��̃L�[�̂悤�Ȋg���L�[���ǂ�����\���B</summary>
		public bool IsExtended {get {return IsFlagging(0x01);} set {Flag(value, 0x01);}}
		///<summary>�C�x���g���C���W�F�N�g���ꂽ���ǂ�����\���B</summary>
		public bool IsInjected {get {return IsFlagging(0x10);} set {Flag(value, 0x10);}}
		///<summary>ALT�L�[��������Ă��邩�ǂ�����\���B</summary>
		public bool AltDown {get {return IsFlagging(0x20);} set {Flag(value, 0x20);}}
		///<summary>�L�[�������ꂽ�ǂ�����\���B</summary>
		public bool IsUp {get {return IsFlagging(0x80);} set {Flag(value, 0x80);}}
	}
	///<summary>�L�[�{�[�h�̑�����t�b�N���A�C�ӂ̃��\�b�h��}������B</summary>
	public class KeyboardHook : Component 
    {
		[DllImport("user32.dll", SetLastError=true)]
		private static extern IntPtr SetWindowsHookEx(int hookType, KeyboardHookDelegate hookDelegate, IntPtr hInstance, uint threadId);
		[DllImport("user32.dll", SetLastError=true)]
		private static extern int CallNextHookEx(IntPtr hook, int code, KeyboardMessage message, ref KeyboardState state);
		[DllImport("user32.dll", SetLastError=true)]
		private static extern bool UnhookWindowsHookEx(IntPtr hook);

		private delegate int KeyboardHookDelegate(int code, KeyboardMessage message, ref KeyboardState state);
		private const int KeyboardHookType = 13;
		private GCHandle hookDelegate;
		private IntPtr hook;
		private static readonly object EventKeyboardHooked = new object();
		///<summary>�L�[�{�[�h�����삳�ꂽ�Ƃ��ɔ�������B</summary>
		public event KeyboardHookedEventHandler KeyboardHooked 
        {
            add { base.Events.AddHandler(EventKeyboardHooked, value); }
            remove { base.Events.RemoveHandler(EventKeyboardHooked, value); }
		}
		///<summary>
		///KeyboardHooked�C�x���g�𔭐�������B
		///</summary>
		///<param name="e">�C�x���g�̃f�[�^�B</param>
		protected virtual void OnKeyboardHooked(KeyboardHookedEventArgs e)
        {
			KeyboardHookedEventHandler handler = base.Events[EventKeyboardHooked] as KeyboardHookedEventHandler;
			if (handler != null)
				handler(this, e);
		}
		///<summary>
		///�V�����C���X�^���X���쐬����B
		///</summary>
		public KeyboardHook()
        {
			if (Environment.OSVersion.Platform != PlatformID.Win32NT)
				throw new PlatformNotSupportedException("Windows 98/Me�ł̓T�|�[�g����Ă��܂���B");
			KeyboardHookDelegate callback = new KeyboardHookDelegate(CallNextHook);
			this.hookDelegate = GCHandle.Alloc(callback);
			IntPtr module = Marshal.GetHINSTANCE(typeof(KeyboardHook).Assembly.GetModules()[0]);
			this.hook = SetWindowsHookEx(KeyboardHookType, callback, module, 0);
		}
		///<summary>
		///�L�[�{�[�h�����삳�ꂽ�Ƃ��Ɏ��s����f���Q�[�g���w�肵�ăC���X�^���X���쐬����B
		///</summary>
		///<param name="handler">�L�[�{�[�h�����삳�ꂽ�Ƃ��Ɏ��s���郁�\�b�h��\���C�x���g�n���h���B</param>
		public KeyboardHook(KeyboardHookedEventHandler handler) : this() 
        {
			this.KeyboardHooked += handler;
		}
		private int CallNextHook(int code, KeyboardMessage message, ref KeyboardState state) 
        {
			if (code >= 0) 
            {
				KeyboardHookedEventArgs e = new KeyboardHookedEventArgs(message, ref state);
				OnKeyboardHooked(e);
				if (e.Cancel) 
                {
					return -1;
				}
			}
			return CallNextHookEx(IntPtr.Zero, code, message, ref state);
		}
		///<summary>
		///�g�p����Ă���A���}�l�[�W���\�[�X��������A�I�v�V�����Ń}�l�[�W���\�[�X���������B
		///</summary>
		///<param name="disposing">�}�l�[�W���\�[�X���������ꍇ��true�B</param>
		protected override void Dispose(bool disposing) 
        {
			if (this.hookDelegate.IsAllocated) 
            {
				UnhookWindowsHookEx(hook);
				this.hook = IntPtr.Zero;
				this.hookDelegate.Free();
			}
			base.Dispose(disposing);
		}
	}
}
