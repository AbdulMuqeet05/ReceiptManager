
import 'package:dio/dio.dart';
import '../client/photo_client.dart';
import 'package:image_picker/image_picker.dart';

class PhotoRepository {
  final PhotoApiClient client;
  PhotoRepository(this.client);

  Future<void> upload(XFile file) async {
    final formData = FormData.fromMap({
      'file': await MultipartFile.fromFile(file.path),
    });
    await client.uploadPhoto(formData);
  }
}
