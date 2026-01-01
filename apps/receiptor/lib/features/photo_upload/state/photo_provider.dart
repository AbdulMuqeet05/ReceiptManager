import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';
import '../model/photo_model.dart';
import '../repository/photo_repository.dart';
import '../client/photo_client.dart';
import 'package:dio/dio.dart';

final photoProvider = StateNotifierProvider<PhotoNotifier, PhotoModel>((ref) {
  return PhotoNotifier(
    PhotoRepository(PhotoApiClient(Dio())),
  );
});

class PhotoNotifier extends StateNotifier<PhotoModel> {
  final PhotoRepository repository;
  final picker = ImagePicker();

  PhotoNotifier(this.repository) : super(const PhotoModel());

  Future<void> pickImage(ImageSource source) async {
    final file = await picker.pickImage(source: source);
    state = state.copyWith(file: file);
  }

  Future<void> upload() async {
    if (state.file == null) return;
    state = state.copyWith(isLoading: true);
    await repository.upload(state.file!);
    state = state.copyWith(isLoading: false);
  }
}
