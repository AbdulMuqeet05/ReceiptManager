import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../state/photo_provider.dart';
import 'package:permission_handler/permission_handler.dart';
import 'package:image_picker/image_picker.dart';

class PhotoScreen extends ConsumerWidget {
  const PhotoScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    Future<bool> _requestPermissions() async {
      final cameraStatus = await Permission.camera.request();
      final photosStatus = await Permission.photos.request();

      if (cameraStatus.isGranted && photosStatus.isGranted) return true;

      if (cameraStatus.isPermanentlyDenied ||
          photosStatus.isPermanentlyDenied) {
        await showDialog(
          context: context,
          builder: (_) => AlertDialog(
            title: Text("Permissions required"),
            content: Text(
                "Camera and Photo Library permissions are required. Please enable them in Settings."),
            actions: [
              TextButton(
                onPressed: () {
                  openAppSettings();
                  Navigator.pop(context);
                },
                child: Text("Open Settings"),
              ),
              TextButton(
                onPressed: () => Navigator.pop(context),
                child: Text("Cancel"),
              ),
            ],
          ),
        );
        return false;
      }

      return false;
    }

    Future<void> _pickImage(ImageSource source) async {
      bool granted = await _requestPermissions();
      if (!granted) return;

      ref.read(photoProvider.notifier).pickImage(source);
    }

    final state = ref.watch(photoProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Receiptor')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: SingleChildScrollView(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              SizedBox(
                height: 24,
              ),
              Center(
                  child: const Text('Upload Receipt',
                      style: TextStyle(fontSize: 20))),
              const SizedBox(height: 8),
              Center(
                  child: const Text(
                      'Pick a receipt and upload it to be analysed')),
              const SizedBox(height: 16),
              if (state.file != null)
                Image.file(
                  File(state.file!.path),
                  height: 500,
                  width: 500,
                ),
              const SizedBox(height: 16),
              ElevatedButton(
                onPressed: () => _pickImage(ImageSource.camera),
                child: const Text('Take Receipt Image with Camera'),
              ),
              ElevatedButton(
                onPressed: () => _pickImage(ImageSource.gallery),
                child: const Text('Select Receipt Image from Gallery'),
              ),
              ElevatedButton(
                onPressed: () => ScaffoldMessenger.of(context)
                    .showSnackBar(SnackBar(content: Text("Not implemented!"))),
                child: const Text('Upload'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
