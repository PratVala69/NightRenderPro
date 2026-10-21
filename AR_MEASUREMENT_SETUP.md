# iOS Build Steps (Xcode)

1. Open the Unity project in **Unity 6000.0.43f1**.
2. Open **File → Build Profiles**, select **iOS**, and switch platform if needed.
3. In **Project Settings → XR Plug-in Management → iOS**, confirm **ARKit** is enabled.
4. In the iOS build profile, confirm the required app scenes are enabled and in the intended order. The checked-in build settings place `MainUIScene` first and `ARMeasurementScene` second.
5. In **Project Settings → Player → iOS**, set the bundle identifier and any required signing, orientation, and deployment settings.
6. Select **Build** and choose an output folder for the generated Xcode project.
7. Open the generated `.xcodeproj` in Xcode and select the app target.
8. In **Build Settings**, find **Other Linker Flags** (`OTHER_LDFLAGS`). Remove the 64-bit linker entry that is being retired from this build configuration. Check both the app target and project-level settings, and verify the active configuration (Debug/Release) so the entry is not inherited from another level.
9. Select the connected iPhone or iPad as the run destination. Set the development team and signing settings under **Signing & Capabilities**.
10. Build and run from Xcode. On first launch, allow camera access so ARKit can start.

## Deploy to TestFlight or the App Store

1. In Xcode, select **Any iOS Device (arm64)** as the run destination.
2. Confirm the app target uses the intended **Release** configuration, bundle identifier, version, and build number. Check **Signing & Capabilities** and select the correct Apple Developer team.
3. Choose **Product → Archive** and wait for the archive to finish. Xcode opens **Organizer** with the new archive.
4. In Organizer, select the archive and choose **Distribute App**.
5. Choose **App Store Connect** to upload for TestFlight or App Store review. Follow the signing and upload prompts, then submit the build to the intended TestFlight group or App Store workflow in App Store Connect.
6. For an in-house or registered-device deployment instead, choose the matching distribution method in the Xcode distribution flow and follow its provisioning/profile prompts.
7. Install and launch the distributed build on a supported device; grant camera access and verify that AR scanning and measurement work in the packaged app.

For a direct development deployment, connect a registered iPhone or iPad, select it as the run destination, and use **Product → Run** after signing is configured.

If Unity regenerates the Xcode project, repeat the **Other Linker Flags** change on the newly generated project unless the change is managed by a Unity post-build script.
