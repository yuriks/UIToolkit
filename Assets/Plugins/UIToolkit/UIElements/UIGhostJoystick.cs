using System;
using System.Collections;
using UnityEngine;

public class UIGhostJoystick : UITouchableSprite
{
	public Vector2 joystickPosition;
	public Vector2 deadZone = Vector2.zero; // Controls when position output occurs
	public bool normalize = true; // Normalize output after the dead-zone?  If true, we start at 0 even though the joystick is moved deadZone pixels already
	public UIUVRect highlightedUVframe = UIUVRect.zero; // Highlighted UV's for the joystick
	public Color fadeRate = Color.clear;
	public bool clamp = true; // Clamp joystick to edge
	
	private UISprite _joystickSprite;
	private UISprite _backgroundSprite;

	private Vector2 _joystickCenter;

	public float maxJoystickMovement = 20.0f; // max distance from _joystickCenter that the joystick will move
	private float resolutionDivisor;
	private UIToolkit _manager; // we need this for getting at texture details after the constructor

	private int currentTouchId = -1;
	
	
	/// <summary>
	/// Hides the all joystick sprites
	/// </summary>
    public override bool hidden
    {
        set
        {
            // No need to do anything if we're already in this state
            if( value == ___hidden )
                return;
			___hidden = value;

			// apply state to the children
			_joystickSprite.hidden = value;
			
			if( _backgroundSprite != null )
				_backgroundSprite.hidden = value;
			base.hidden = value;
        }
    }

	public override bool disabled
	{
		set
		{
			if (value == _disabled)
				return;
			_disabled = value;

			resetJoystick();
		}
	}
	
	
	public static UIGhostJoystick create( string joystickFilename, Rect hitArea )
	{
		return create( UI.firstToolkit, joystickFilename, hitArea );
	}

	
	public static UIGhostJoystick create( UIToolkit manager, string joystickFilename, Rect hitArea )
	{
		// create the joystrick sprite
		var joystick = manager.addSprite( joystickFilename, 0, 0, 1, true );
		
		return new UIGhostJoystick( manager, hitArea, 1, joystick);
	}

	
	public UIGhostJoystick( UIToolkit manager, Rect frame, int depth, UISprite joystickSprite)
		: base(frame, depth, UIUVRect.zero)
	{
		// Save out the uvFrame for the sprite so we can highlight
		_tempUVframe = joystickSprite.uvFrame;
		
		// Save the joystickSprite and make it a child of the us for organization purposes
		_joystickSprite = joystickSprite;
		_joystickSprite.parentUIObject = this;
		
		resetJoystick();
		
		manager.addTouchableSprite( this );
		_manager = manager;

		resolutionDivisor = UIRelative.pixelDensityMultiplier();
	}
	
	// Sets the image to be displayed when the joystick is highlighted
	public void setJoystickHighlightedFilename( string filename )
	{
		var textureInfo = _manager.textureInfoForFilename( filename );
		highlightedUVframe = textureInfo.uvRect;
	}
	

	// Sets the background image for display behind the joystick sprite
	public void addBackgroundSprite( string filename )
	{
		_backgroundSprite = _manager.addSprite( filename, 0, 0, 2, true );
		_backgroundSprite.parentUIObject = this;
		_backgroundSprite.hidden = true;
	}
	
	
	// Resets the sprite to default position and zeros out the position vector
	private void resetJoystick()
	{
		_joystickSprite.localPosition = _joystickCenter;
		joystickPosition.x = joystickPosition.y = 0.0f;
		
		// If we have a highlightedUVframe, swap the original back in
		if( highlightedUVframe != UIUVRect.zero )
			_joystickSprite.uvFrame = _tempUVframe;

		if (_backgroundSprite != null) {
			_backgroundSprite.hidden = true;
		}
		_joystickSprite.localPosition = new Vector3(-1000, -1000, -1000);
		_joystickSprite.hidden = true;

		highlighted = false;
		currentTouchId = -1;
	}

	private void hideJoystick() {
		if (fadeRate == Color.clear) {
			resetJoystick();
		} else {
			joystickPosition.x = joystickPosition.y = 0.0f;

			// If we have a highlightedUVframe, swap the original back in
			if( highlightedUVframe != UIUVRect.zero )
				_joystickSprite.uvFrame = _tempUVframe;

			UI.instance.StartCoroutine(animateFadeOut());
		}
	}

	private static Color clampColor(Color c) {
		if (c.r <= 0f)
			c.r = 0f;
		if (c.g <= 0f)
			c.g = 0f;
		if (c.b <= 0f)
			c.b = 0f;
		if (c.a <= 0f)
			c.a = 0f;
		return c;
	}

	private IEnumerator animateFadeOut() {
		Color tmp;

		while (currentTouchId == -1) {
			yield return null;

			bool quit = false;

			tmp = _joystickSprite.color;
			tmp = clampColor(tmp - fadeRate * Time.deltaTime);
			if (tmp == Color.clear) {
				quit = true;
			}
			_joystickSprite.color = tmp;

			if (_backgroundSprite != null) {
				tmp = _backgroundSprite.color;
				tmp = clampColor(tmp - fadeRate * Time.deltaTime);
				_backgroundSprite.color = tmp;
			}

			if (quit) {
				break;
			}
		}

		_joystickSprite.color = Color.white;
		if (_backgroundSprite != null) {
			_backgroundSprite.color = Color.white;
		}

		if (currentTouchId == -1) {
			resetJoystick();
		}
	}

	private void displayJoystick(Vector2 localTouchPos)
	{
		if (clamp) {
			float edge = maxJoystickMovement*resolutionDivisor * 1.3f;

			if (localTouchPos.x - edge < 0f)
				localTouchPos.x = edge;
			else if (localTouchPos.x + edge > Screen.width)
				localTouchPos.x = Screen.width - edge;

			if (localTouchPos.y + edge >= 0f)
				localTouchPos.y = -edge;
			else if (localTouchPos.y - edge <= -Screen.height)
				localTouchPos.y = edge - Screen.height;
		}

		_joystickCenter = localTouchPos;

		_joystickSprite.localPosition = _joystickCenter;
		joystickPosition.x = joystickPosition.y = 0.0f;
		if (_backgroundSprite != null) {
			_backgroundSprite.localPosition = new Vector3( _joystickCenter.x, _joystickCenter.y, 2 );
			_backgroundSprite.hidden = false;
		}
		_joystickSprite.hidden = false;
	}

	private void layoutJoystick( Vector2 localTouchPosition )
	{
		// Clamp the new position based on the boundaries we have set.  Dont forget to reverse the Y axis!
		Vector2 newPosition = localTouchPosition;

		float sqrlen = newPosition.sqrMagnitude;
		if (sqrlen > maxJoystickMovement*maxJoystickMovement*resolutionDivisor*resolutionDivisor) {
			newPosition = newPosition.normalized * maxJoystickMovement*resolutionDivisor;
		}
		
		// Set the new position and update the transform
		_joystickSprite.localPosition = new Vector2(newPosition.x + _joystickCenter.x, _joystickCenter.y + newPosition.y);
		
		// Get a value between -1 and 1 for position
		joystickPosition = newPosition / (maxJoystickMovement*resolutionDivisor);
		
		// Adjust for dead zone	
		float absoluteX = Mathf.Abs( joystickPosition.x );
		float absoluteY = Mathf.Abs( joystickPosition.y );
	
		if( absoluteX < deadZone.x )
		{
			// Report the joystick as being at the center if it is within the dead zone
			joystickPosition.x = 0;
		}
		else if( normalize )
		{
			// Rescale the output after taking the dead zone into account
			joystickPosition.x = Mathf.Sign(joystickPosition.x) * (absoluteX - deadZone.x) / (1 - deadZone.x);
		}
		
		if( absoluteY < deadZone.y )
		{
			// Report the joystick as being at the center if it is within the dead zone
			joystickPosition.y = 0;
		}
		else if( normalize )
		{
			// Rescale the output after taking the dead zone into account
			joystickPosition.y = Mathf.Sign(joystickPosition.y) * (absoluteY - deadZone.y) / (1 - deadZone.y);
		}
	}
	

#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WEBPLAYER
	public override void onTouchBegan( UIFakeTouch touch, Vector2 touchPos )
#else
	public override void onTouchBegan( Touch touch, Vector2 touchPos )
#endif
	{
		if (currentTouchId != -1)
			return;

		if (disabled)
			return;

		currentTouchId = touch.fingerId;

		touchPos.y = -touchPos.y;

		highlighted = true;

		// Re-center joystick pad
		displayJoystick(touchPos);

		this.layoutJoystick( this.inverseTranformPoint(touchPos - _joystickCenter));
		
		// If we have a highlightedUVframe, swap it in
		if( highlightedUVframe != UIUVRect.zero )
			_joystickSprite.uvFrame = highlightedUVframe;
	}
	

#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WEBPLAYER
	public override void onTouchMoved( UIFakeTouch touch, Vector2 touchPos )
#else
	public override void onTouchMoved( Touch touch, Vector2 touchPos )
#endif
	{
		if (touch.fingerId != currentTouchId)
			return;

		touchPos.y = -touchPos.y;

		this.layoutJoystick(this.inverseTranformPoint(touchPos - _joystickCenter));
	}
	

#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WEBPLAYER
	public override void onTouchEnded( UIFakeTouch touch, Vector2 touchPos, bool touchWasInsideTouchFrame )
#else
	public override void onTouchEnded( Touch touch, Vector2 touchPos, bool touchWasInsideTouchFrame )
#endif
	{
		if (touch.fingerId != currentTouchId)
			return;

		// Set highlighted to avoid calling super
		highlighted = false;
		
		currentTouchId = -1;

		// Reset back to default state
		this.hideJoystick();
	}
	
}


