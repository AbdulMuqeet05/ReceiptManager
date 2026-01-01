
import 'package:freezed_annotation/freezed_annotation.dart';
import 'package:image_picker/image_picker.dart';

part 'photo_model.freezed.dart';

@freezed
class PhotoModel with _$PhotoModel {
  const factory PhotoModel({
    XFile? file,
    @Default(false) bool isLoading,
  }) = _PhotoModel;
}
