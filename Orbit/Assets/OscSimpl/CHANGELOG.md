2.3.2 (2020/10/15)

**CHANGES**

* Reduced garbage created by OscMessage.ToString() and thereby greatly improved performance of inspectors.
* Added byte count monitoring for OscIn and OscOut.
* Optimised internal buffer handling.
* Fixed bug in OscMessage.RemoveAt().
* Moved message sending from EndOfFrame to LateUpdate. To send as fast as possible, call OscOut.Send() in Update.
* Deprecated OscOut.multicastLoopback. This feature was never working as expected because C# Socket.multicastLoopback only affects the same socket.


2.3.1 (2020/7/23)

**CHANGES**

* Added OscOut.bundleMessagesAutomatically. True by default. Only set to false if the receiving end does not support OSC bundles. Unbundled messages that are send successively are prone to be lost.


2.3.0 (2020/06/17)
------------------

**FEATURES**

* Speed! Expect more than x 3 performance and still no garbage.


**IMPROVEMENTS**

* Optimized string hashing and parsing, OscIn.filterDuplicates checks, OscMapping.hasSpecialPattern, OscMessage.Set() and OscMessage data structure.
* Added OscMessage.TryGetBlob() support for Vector2Int, Vector3Int and Rect.

**CHANGES**

* Changed OscIn.filterDuplicates default to false. It was causing confusion.
* Removed obsolete methods in OscOut and OscMessage.
* Partially adopted Unity's default package layout.


2.2.2 (2020/04/11)
------------------

**FEATURES**

* Added OscGlobals, a Singleton Monobehaviour containing global settings and stats. For now it holds logStatuses and logWarnings flags. Use is optional.

**IMPROVEMENTS**

* Updated log messages.
* Removed a costly check in OscPool. Only call OscPool.Recycle() once per message.


2.2.1 (2019/12/14)
------------------

**INTERNAL**

* To ensure that individual messages that are sent immediately after each other behave as expected, they are now bundled and sent OnEndOfFrame by default. For this reason, the OscOut setting bundleMessagesOnEndOfFrame has been marked as obsolete. If you want to send messages immediately, without waiting for the frame to end, then you can collect them in a bundle and send that in one Send call.
* The OscOut setting splitBundlesAvoidingBufferOverflow is now always true and has therefore been marked as obsolete.


2.2.0 (2019/11/30)
------------------

This is a minor update without code breaking changes. However, mappings you have made in the Editor OscIn inspector may be forgotten, so you may have to set them up again. Sorry for the inconvenience.

**FEATURES**

* The OscIn.Map methods can now bind methods that are not members of UnityEngine.Object and anonymous methods as well.

**IMPROVEMENTS**

* Redesigned the OscIn inspector Mappings section.
* Removed onAnyMessage in OscIn and OscOut. The methods have been marked obsolete since version 2.0.0. Use MapAnyMessage and UnmapAnyMessage instead.

**INTERNAL**

* Replaced UnityEvent with a custom solution for storing and invoking OscMappings.
* Improved OscIn.localIpAddress reliability.
* Started using semantic versioning as promoted for Unity packages: MAJOR.MINOR.PATCH.

**FIXES**

* Fixed iOS build issue concerning OscIn mappings when they are defined in the inspector.


2.1.2 (2019/10/06)
------------------

**INTERNAL**

* Fixed inspector foldout toggles not working in Unity 2019.3.


2.1.1 (2019/09/02)
------------------

**INTERNAL**

* Fixed message pooling bug that produced garbage in some cases.


2.1.0 (2019/08/30)
------------------

**FEATURES**

* Added OscIn.localIpAddressAlternatives to get additional local IP addresses when your device has multiple network adapters connected.

**INTERNAL**

* Optimised bundleMessagesOnEndOfFrame by avoiding DateTime.Now call.
* Optimised OscMessage.SetBlob.
* Integrated ExposedUnityEvents as OscEvent to avoid conflicts with other assets.
* Set to ignore "Host is down" warning when seding to a non-existent remote target.


2.0.0 (2019/01/26)
------------------
This is a major update that can in worst case raise errors when upgrading existing projects. An upgrade guide is posted on the [forum](https://bit.ly/2G2FbAG).


**FEATURES**

* Added full two-way support for OSC 1.0 Address Pattern Matching.
* Added OscMessage argument type: OscMidiMessage.
* Added OscMessage methods for containing common Unity types in blobs.
* Added udpBufferSize setting for OscIn and OscOut.
* Added OscOut.splitBundlesAvoidingBufferOverflow option.

**IMPROVEMENTS**

* Updated examples to promote less heap memory garbage generation.
* Added message recycling to avoid garbage. Call OscPool.Recycle( message )when you are done with a received messages in your scripts.
* Streamlined inspector UI for OscIn mappings.
* Added method chaining to OscMessage.Add, also available for Set and SetBlob.

**CHANGES**

* Scripting Runtime Version .NET 3.5 is no longer supported. Set Unity to use .NET 4.0 in the player settings.
* Deprecated OscIn.ipAddress. Use OscIn.localIpAddress instead.
* Deprecated OscOut.ipAddress. Use OscIn.remoteIpAddress instead.
* Deprecated onAnyMessage and added MapAnyMessage and UnmapAnyMessage.
* Deprecated versions of OscMessage constructor and OscMessage.Add that accepts params object[] args.
* Deprecated TryGetNull, TryGetImpulse and replaced with versions of TryGet to streamline method naming.
* Removed Map() support for delegates, methods must be defined in objects that derive from ScriptableObject or MonoBehaviour.
* Removed OscMessage.args and added Set(), Count(), TryGetArgType() and RemoveAt methods.


1.3.0 (2018/05/31)
------------------

* Fixed 2018.2 issue. Network class had ben removed. Switched to Dns class for retrieving local ip address.


1.2.0 (2016/12/10)
------------------

* Added workaround for Unity 5.5 overload method issue. Update note Your oscIn.Map and oscIn.Unmap calls may need need renaming. For example osc.Map( SomeFloatArgMethod ) is now osc.MapFloat( SomeFloatArgMethod ).Please see the updated reference for more info. [About the issue](https://forum.unity3d.com/threads/445139)
* Extended troubleshooting section in Manual.pdf.
* Removed oscMessage.TryGet( int index, out Color value ) because it is misleading. OSC only supports Color32.
* Removed oscMessage.TryGet( int index, out DateTime value ) because it is misleading. Use TryGet( int index, out OscTimeTag value ).
* Prevented the OscIn inspector from stalling Unity 5.5. OscIn.ipAddress now uses native Unity code.


1.1.0 (2016/02/04)
------------------

* Updated Manual.pdf.
* Fixed draw order problem in OscIn inspector causing mappings to be occluded on some systems.


1.0.0 (2016/01/26)
------------------

* First release.
