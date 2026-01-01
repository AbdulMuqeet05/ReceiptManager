
import 'package:dio/dio.dart';

class PhotoApiClient {
  final Dio dio;
  PhotoApiClient(this.dio);

  Future<void> uploadPhoto(FormData data) async {
    await dio.post('https://example.com/upload', data: data);
  }
}
